using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Users;

namespace Platform.Legacy.Data.Entities.Planners;

/// <summary>
/// Planner Submission
/// Composite Index of CompanyId x Status
/// Composite Index of CreatedOn x Status
/// </summary>
[Index(propertyName: nameof(PlannerSubmission.CompanyId), nameof(PlannerSubmission.Status))]
public class PlannerSubmission : IEntityTypeConfiguration<PlannerSubmission>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<PlannerSubmission> { }

	// Primary properties

	/// <summary>
	/// Record Id
	/// </summary>
	public int Id { get; init; }

	/// <summary>
	/// Submission name
	/// </summary>
	[MaxLength(255)]
	public string? Name { get; set; }

	/// <summary>
	/// Copy of Planner Schema
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? PlannerSchemaJson { get; set; }

	/// <summary>
	/// Submission Schema
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? PlannerSubmissionSchemaJson { get; set; }

	/// <summary>
	/// Status
	/// WARNING THIS IS AN ENUM, CHANGING ENUM ENTRY ORDER IN CODE WILL BREAK DB
	/// </summary>
	public PlannerSubmissionStatus Status { get; set; } = PlannerSubmissionStatus.Draft;

	/// <summary>
	/// Search Tags
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? Tags { get; set; }

	/// <summary>
	/// Version
	/// </summary>
	public int Version { get; set; }

	// Soft Delete properties
	/// <summary>
	/// If the planner is deleted (Replace with null check on DeletedOn)
	/// </summary>
	public bool IsDeleted { get; set; } // bool defaults to false

	/// <summary>
	/// Deleted On
	/// </summary>
	public DateTime? DeletedOn { get; set; }

	/// <summary>
	/// Deleted By User
	/// </summary>
	public int? DeletedById { get; set; }

	// Foreign keys

	/// <summary>
	/// Planner
	/// </summary>
	public int PlannerId { get; set; }

	/// <summary>
	/// Planner Assignment
	/// </summary>
	public int? PlannerAssignmentId { get; set; }

	/// <summary>
	/// Planner Due Date
	/// </summary>
	public int? PlannerDueDateId { get; set; }

	/// <summary>
	/// Company
	/// </summary>
	public int? CompanyId { get; set; }

	// Audit properties

	/// <summary>
	/// Created On
	/// </summary>
	public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Created By user
	/// </summary>
	public int CreatedById { get; set; }

	/// <summary>
	/// Update on
	/// </summary>
	public DateTime? UpdatedOn { get; set; }

	/// <summary>
	/// Updated By user
	/// </summary>
	public int? UpdatedById { get; set; }

	/// <summary>
	/// Completed On
	/// </summary>
	public DateTime? CompletedOn { get; set; }

	/// <summary>
	/// Completed By user
	/// </summary>
	public int? CompletedById { get; set; }

	// Navigation properties

	/// <summary>
	/// Planner
	/// </summary>
	public Planner Planner
	{
		get => planner ?? throw new NavigationUnloadedException();
		set => planner = value;
	}
	private Planner? planner;

	/// <summary>
	/// Planner Assignment
	/// </summary>
	public PlannerAssignment? PlannerAssignment { get; set; }

	/// <summary>
	/// Planner Due Date
	/// </summary>
	public PlannerDueDate? PlannerDueDate { get; set; }

	/// <summary>
	/// Created By user
	/// </summary>
	public User CreatedBy
	{
		get => createdBy ?? throw new NavigationUnloadedException();
		set => createdBy = value;
	}
	private User? createdBy;

	/// <summary>
	/// Updated By User
	/// </summary>
	public User? UpdatedBy { get; set; }

	/// <summary>
	/// Completed By User
	/// </summary>
	public User? CompletedBy { get; set; }

	/// <summary>
	/// Deleted By user
	/// </summary>
	public User? DeletedBy { get; set; }

	// Added since computing the completion percentage from tasks is computationally complex enough
	// that we should be computing on update and storing the result, rather than recomputing on retreival

	/// <summary>
	/// completion percentage (out of 100?)
	/// </summary>
	public int CompletionPercentage { get; set; }

	// USE NOT MAPPED ATTRIBUTE FOR PROPERTIES THAT SHOULD NOT HAVE EF COLUMNS

	/// <summary>
	/// If the submission is completed
	/// </summary>
	[NotMapped]
	public bool IsCompleted => Status == PlannerSubmissionStatus.Completed && CompletedOn.HasValue;

	/// <summary>
	/// If the submission can be modified
	/// </summary>
	[NotMapped]
	public bool CanBeModified =>
		!IsDeleted
		&& Status != PlannerSubmissionStatus.Completed
		&& Status != PlannerSubmissionStatus.Cancelled
		&& Status != PlannerSubmissionStatus.Deleted;

	/// <summary>
	/// If the submission can be completed
	/// </summary>
	[NotMapped]
	public bool CanBeCompleted =>
		Status == PlannerSubmissionStatus.Draft && !string.IsNullOrEmpty(PlannerSubmissionSchemaJson);

	/// <summary>
	/// If the submission is overdue
	/// </summary>
	[NotMapped]
	public bool IsOverdue =>
		PlannerDueDate != null
		&& PlannerDueDate.DueDateTime < DateTime.UtcNow
		&& Status != PlannerSubmissionStatus.Completed
		&& Status != PlannerSubmissionStatus.Cancelled
		&& Status != PlannerSubmissionStatus.Deleted;

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<PlannerSubmission> builder)
	{
		/* See previous comments
		builder.HasKey(ps => ps.Id);
		builder.Property(ps => ps.Name).HasMaxLength(255);
		builder.Property(ps => ps.PlannerSchemaJson).HasColumnType("text");
		builder.Property(ps => ps.PlannerSubmissionSchemaJson).HasColumnType("text");
		builder.Property(ps => ps.Tags).HasColumnType("text");
		builder.Property(ps => ps.Status).IsRequired();
		builder.Property(ps => ps.CreatedOn).IsRequired();
		builder.Property(ps => ps.CreatedById).IsRequired();
		builder.Property(ps => ps.UpdatedOn).IsRequired();
		builder.Property(ps => ps.PlannerId).IsRequired();
		builder.Property(ps => ps.Version).IsRequired();
		*/

		// Avoid using db defaults unless necessary
		// Also bool defaults to false
		//builder.Property(ps => ps.IsDeleted).HasDefaultValue(false);

		// Doubly defined this relationship, both will be in effect and will cause unexpected issues
		// I've commented out the one on the planner / planner due date / planner assignment side
		builder
			.HasOne(ps => ps.Planner)
			.WithMany(p => p.Submissions)
			.HasForeignKey(ps => ps.PlannerId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne(ps => ps.PlannerAssignment)
			.WithMany(pa => pa.Submissions)
			.HasForeignKey(ps => ps.PlannerAssignmentId)
			.OnDelete(DeleteBehavior.SetNull);

		builder
			.HasOne(ps => ps.PlannerDueDate)
			.WithMany(pdd => pdd.Submissions)
			.HasForeignKey(ps => ps.PlannerDueDateId)
			.OnDelete(DeleteBehavior.SetNull);

		builder
			.HasOne(ps => ps.CreatedBy)
			.WithMany(x => x.CreatedPlannerSubmissions) // Define the other side of the relationship navigation
			.HasForeignKey(ps => ps.CreatedById)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne(ps => ps.UpdatedBy)
			.WithMany(x => x.UpdatedPlannerSubmissions) // Define the other side of the relationship navigation
			.HasForeignKey(ps => ps.UpdatedById)
			.OnDelete(DeleteBehavior.SetNull);

		builder
			.HasOne(ps => ps.CompletedBy)
			.WithMany(x => x.CompletedPlannerSubmissions) // Define the other side of the relationship navigation
			.HasForeignKey(ps => ps.CompletedById)
			.OnDelete(DeleteBehavior.SetNull);

		builder
			.HasOne(ps => ps.DeletedBy)
			.WithMany(x => x.DeletedPlannerSubmissions) // Define the other side of the relationship navigation
			.HasForeignKey(ps => ps.DeletedById)
			.OnDelete(DeleteBehavior.SetNull);

		/* Set via attributes on the entity class itself
		 * Out of curiosity what drove the need for these composite indexes?
		builder.HasIndex(ps => new { ps.CompanyId, ps.Status });
		builder.HasIndex(ps => new { ps.CreatedOn, ps.Status });
		*/

		// INDEXING A BLOB OR TEXT ON MYSQL IS A BAD IDEA
		// NEED TO CONVERT TO VARCHAR BY SETTING A MAXLENGTH!
		// REMOVING INDEX FOR NOW
		//builder.HasIndex(ps => ps.Tags);
	}
}
