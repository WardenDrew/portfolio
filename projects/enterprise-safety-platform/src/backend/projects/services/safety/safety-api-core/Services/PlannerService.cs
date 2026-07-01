using System.Text.Json.Nodes;
using Platform.Legacy.Data.Entities.Planners;

namespace Platform.Legacy.Core.Services;

/// <summary>
/// Service for planner related common functionality
/// </summary>
/// <param name="db"></param>
public class PlannerService(LegacyDbContext db)
{
	/// <summary>
	/// Calculate the completion percentage of a Planner Submission
	/// </summary>
	/// <param name="submission"></param>
	/// <returns></returns>
	public int CalculateCompletionPercentage(PlannerSubmission submission)
	{
		// ReSharper disable once ConvertIfStatementToSwitchStatement
		if (submission.Status == PlannerSubmissionStatus.Completed)
		{
			return 100;
		}

		if (submission.Status is PlannerSubmissionStatus.Cancelled or PlannerSubmissionStatus.Deleted)
		{
			return 0;
		}

		if (string.IsNullOrEmpty(submission.PlannerSubmissionSchemaJson))
		{
			return 0;
		}

		JsonNode? jsonNode = JsonNode.Parse(submission.PlannerSubmissionSchemaJson);
		if (jsonNode == null)
		{
			return 0;
		}

		// Handle task-type planners
		if (submission.Planner.Type?.ToLower() == "task")
		{
			JsonArray? tasks = jsonNode["tasks"]?.AsArray();
			if (tasks == null || tasks.Count == 0)
			{
				return 0;
			}

			int totalTasks = tasks.Count;
			int completedTasks = tasks.Count(task => task?["status"]?.GetValue<string>() == "done");

			return totalTasks > 0 ? (int)Math.Round((double)completedTasks / totalTasks * 100) : 0;
		}

		// Handle inspection-type planners
		if (submission.Planner.Type?.ToLower() == "inspection")
		{
			JsonArray? inspectionCriteria = jsonNode["inspectionCriteria"]?.AsArray();
			if (inspectionCriteria == null || inspectionCriteria.Count == 0)
			{
				return 0;
			}

			int totalCriteria = inspectionCriteria.Count;
			int verifiedCriteria = inspectionCriteria.Count(criteria =>
				criteria?["status"]?.GetValue<string>() == "verified"
			);

			return totalCriteria > 0 ? (int)Math.Round((double)verifiedCriteria / totalCriteria * 100) : 0;
		}

		// Handle hazard-type planners & reports
		JsonArray? hazards = jsonNode["hazards"]?.AsArray();
		if (hazards == null || hazards.Count == 0)
		{
			return 0;
		}

		int totalControls = 0;
		int completedControls = 0;

		foreach (JsonNode? hazard in hazards)
		{
			JsonArray? subTypes = hazard?["subTypes"]?.AsArray();
			if (subTypes == null)
			{
				continue;
			}

			foreach (JsonNode? subType in subTypes)
			{
				JsonObject? status = subType?["status"]?.AsObject();
				if (status == null)
				{
					continue;
				}

				foreach (KeyValuePair<string, JsonNode?> control in status.AsObject())
				{
					totalControls++;
					if (control.Value?.GetValue<string>() == "done")
					{
						completedControls++;
					}
				}
			}
		}

		return totalControls > 0 ? (int)Math.Round((double)completedControls / totalControls * 100) : 0;
	}

	/// <summary>
	/// Recalculate Planner Assignment Status
	/// </summary>
	/// <exception cref="InvalidOperationException">Throws when the plannerAssignment is loaded without tracking and a change is needed</exception>
	/// <returns></returns>
	public async Task RecalculatePlannerAssignmentStatusAsync(
		PlannerAssignment plannerAssignment,
		CancellationToken cancellationToken = default
	)
	{
		if (plannerAssignment.Status is PlannerAssignmentStatus.Cancelled or PlannerAssignmentStatus.Deleted)
		{
			return;
		}

		bool hasSubmissions = await db.Set<PlannerSubmission>()
			.AsNoTracking()
			.Where(x => x.PlannerAssignmentId == plannerAssignment.Id)
			.AnyAsync(cancellationToken);
		if (hasSubmissions)
		{
			await UpdatePlannerAssignmentStatusAsync(
				plannerAssignment: plannerAssignment,
				correctStatus: PlannerAssignmentStatus.Draft,
				cancellationToken: cancellationToken
			);
			return;
		}

		List<PlannerDueDate> dueDates = await db.Set<PlannerDueDate>()
			.AsNoTracking()
			.Where(x => x.PlannerAssignmentId == plannerAssignment.Id)
			.ToListAsync(cancellationToken);
		if (dueDates.Count != 0)
		{
			if (
				plannerAssignment.EndDate < DateTime.UtcNow
				&& plannerAssignment.Status != PlannerAssignmentStatus.Completed
			)
			{
				await UpdatePlannerAssignmentStatusAsync(
					plannerAssignment: plannerAssignment,
					correctStatus: PlannerAssignmentStatus.InProgress,
					cancellationToken: cancellationToken
				);
			}

			return;
		}

		bool allDueDatesCompleted = dueDates.All(d => d.Status == PlannerDueDateStatus.Completed);
		if (allDueDatesCompleted)
		{
			await UpdatePlannerAssignmentStatusAsync(
				plannerAssignment: plannerAssignment,
				correctStatus: PlannerAssignmentStatus.Completed,
				cancellationToken: cancellationToken
			);
			return;
		}

		bool hasAnyMissed = dueDates.Any(d => d.Status == PlannerDueDateStatus.Overdue);
		bool hasUpcoming = dueDates.Any(d => d.Status == PlannerDueDateStatus.Pending);
		if (hasAnyMissed || hasUpcoming)
		{
			await UpdatePlannerAssignmentStatusAsync(
				plannerAssignment: plannerAssignment,
				correctStatus: PlannerAssignmentStatus.InProgress,
				cancellationToken: cancellationToken
			);
		}
	}

	/// <summary>
	/// Used by during RecalculatePlannerAssignmentStatus
	/// </summary>
	/// <param name="plannerAssignment"></param>
	/// <param name="correctStatus"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	private async Task UpdatePlannerAssignmentStatusAsync(
		PlannerAssignment plannerAssignment,
		PlannerAssignmentStatus correctStatus,
		CancellationToken cancellationToken = default
	)
	{
		if (correctStatus == plannerAssignment.Status)
		{
			return;
		}

		if (!db.IsAttached(plannerAssignment))
		{
			throw new InvalidOperationException(
				"Planner Assignment Status needs to be recalculated, however, the assignment was loaded "
					+ "with AsNoTrackign()!"
			);
		}

		plannerAssignment.Status = correctStatus;
		await db.SaveChangesAsync(cancellationToken);
	}

	// TO FIX

	// Planner

	// public void ArchivePlanner()
	// {
	// 	Status = PlannerStatus.Archived;
	// 	UpdatedOn = DateTime.UtcNow;
	// }
	//
	// public void ActivatePlanner()
	// {
	// 	Status = PlannerStatus.Active;
	// 	UpdatedOn = DateTime.UtcNow;
	// }
	//
	// public void DeletePlanner()
	// {
	// 	Status = PlannerStatus.Deleted;
	// 	UpdatedOn = DateTime.UtcNow;
	// }
	//
	// public void AddPlannerTag(string tag)
	// {
	// 	List<string>? tags = Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
	// 	if (!tags.Contains(tag))
	// 	{
	// 		tags.Add(tag);
	// 		Tags = string.Join(',', tags);
	// 		UpdatedOn = DateTime.UtcNow;
	// 	}
	// }
	//
	// public void RemovePlannerTag(string tag)
	// {
	// 	List<string>? tags = Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
	// 	if (tags.Remove(tag))
	// 	{
	// 		Tags = string.Join(',', tags);
	// 		UpdatedOn = DateTime.UtcNow;
	// 	}
	// }
	//
	// // Planner Assignment
	//
	//
	//
	// // Planner Due Date
	//
	// public DueDateStatus CalculateEffectivePlannerDueDateStatus()
	// {
	// 	if (Status == DueDateStatus.Cancelled)
	// 		return Status;
	//
	// 	if (Status == DueDateStatus.Completed)
	// 		return DueDateStatus.Completed;
	//
	// 	return DueDateTime < DateTime.UtcNow
	// 		? DueDateStatus.Overdue
	// 		: DueDateStatus.Pending;
	// }
	//
	// public void UpdatePlannerDueDateStatus()
	// {
	// 	if (Status == DueDateStatus.Completed || Status == DueDateStatus.Cancelled)
	// 		return;
	//
	// 	Status = DueDateTime < DateTime.UtcNow ? DueDateStatus.Overdue : DueDateStatus.Pending;
	// 	UpdatedOn = DateTime.UtcNow;
	// }
	//
	// public void MarkPlannerDueDateAsCompleted()
	// {
	// 	if (Status == DueDateStatus.Completed)
	// 		return;
	//
	// 	Status = DueDateStatus.Completed;
	// 	CompletedOn = DateTime.UtcNow;
	// 	UpdatedOn = DateTime.UtcNow;
	// }
	//
	// public void CancelPlannerDueDate()
	// {
	// 	if (Status == DueDateStatus.Cancelled)
	// 		return;
	//
	// 	Status = DueDateStatus.Cancelled;
	// 	UpdatedOn = DateTime.UtcNow;
	// }
	//
	// // Planner Submission
	//
	// public void CompletePlannerSubmission(int userId)
	// {
	// 	if (Status == PlannerSubmissionStatus.Completed || IsDeleted)
	// 		return;
	//
	// 	Status = PlannerSubmissionStatus.Completed;
	// 	CompletedOn = DateTime.UtcNow;
	// 	CompletedById = userId;
	// 	UpdateAudit(userId);
	// }
	//
	// public void CancelPlannerSubmission(int userId)
	// {
	// 	if (Status == PlannerSubmissionStatus.Cancelled || IsDeleted)
	// 		return;
	//
	// 	Status = PlannerSubmissionStatus.Cancelled;
	// 	UpdateAudit(userId);
	// }
	//
	// public void DeletePlannerSubmission(int userId)
	// {
	// 	if (IsDeleted)
	// 		return;
	//
	// 	IsDeleted = true;
	// 	DeletedOn = DateTime.UtcNow;
	// 	DeletedById = userId;
	// 	Status = PlannerSubmissionStatus.Deleted;
	// 	UpdateAudit(userId);
	// }
	//
	// public void ReactivatePlannerSubmission(int userId)
	// {
	// 	if (Status == PlannerSubmissionStatus.Draft)
	// 		return;
	//
	// 	if (Status == PlannerSubmissionStatus.Deleted)
	// 		throw new InvalidOperationException("Cannot reactivate a deleted submission.");
	//
	// 	Status = PlannerSubmissionStatus.Draft;
	// 	UpdateAudit(userId);
	// }
	//
	// public void AddPlannerSubmissionTag(string tag)
	// {
	// 	if (IsDeleted) return;
	//
	// 	List<string>? tags = Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
	// 	if (!tags.Contains(tag))
	// 	{
	// 		tags.Add(tag);
	// 		Tags = string.Join(',', tags);
	// 		UpdatedOn = DateTime.UtcNow;
	// 	}
	// }
	//
	// public void RemovePlannerSubmissionTag(string tag)
	// {
	// 	if (IsDeleted) return;
	//
	// 	List<string>? tags = Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
	// 	if (tags.Remove(tag))
	// 	{
	// 		Tags = string.Join(',', tags);
	// 		UpdatedOn = DateTime.UtcNow;
	// 	}
	// }
	//
	// private void UpdatePlannerSubmissionAudit(int userId)
	// {
	// 	UpdatedOn = DateTime.UtcNow;
	// 	UpdatedById = userId;
	// 	Version++;
	// }
	//
	// private static int CountPlannerSubmissionTasks(JsonElement element)
	// {
	// 	if (!element.TryGetProperty(propertyName: "tasks", out JsonElement tasks) || tasks.ValueKind != JsonValueKind.Array)
	// 	{
	// 		return 0;
	// 	}
	//
	// 	return tasks.GetArrayLength();
	// }
	//
	// private static int CountCompletedPlannerSubmissionTasks(JsonElement element)
	// {
	// 	if (!element.TryGetProperty(propertyName: "tasks", out JsonElement tasks) || tasks.ValueKind != JsonValueKind.Array)
	// 	{
	// 		return 0;
	// 	}
	//
	// 	int completedTasks = tasks.EnumerateArray()
	// 		.Count(task => task.TryGetProperty(propertyName: "status", out JsonElement status) &&
	// 					   status.GetString() == "Completed");
	//
	// 	return completedTasks;
	// }
}
