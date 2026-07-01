using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Platform.Legacy.Data.Entities.Planners;

/// <summary>
/// Planner Due Date
/// </summary>
public class PlannerDueDate : IEntityTypeConfiguration<PlannerDueDate>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<PlannerDueDate> { }

	/// <summary>
	/// Record Id
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Planner Assignment
	/// </summary>
	public required int PlannerAssignmentId { get; set; }

	/// <summary>
	/// Due Date
	/// </summary>
	public DateTime DueDateTime { get; set; }

	/// <summary>
	/// Status
	/// </summary>
	public PlannerDueDateStatus Status { get; set; } = PlannerDueDateStatus.Pending;

	/// <summary>
	/// Completed On
	/// </summary>
	public DateTime? CompletedOn { get; set; }

	/// <summary>
	/// Created On
	/// </summary>
	public DateTime CreatedOn { get; set; }

	/// <summary>
	/// Updated On
	/// </summary>
	public DateTime UpdatedOn { get; set; }

	/// <summary>
	/// Notes
	/// </summary>
	[MaxLength(500)]
	public string? Notes { get; set; }

	/// <summary>
	/// Requires Safety Compliance
	/// </summary>
	public bool RequiresSafetyCompliance { get; set; } = true;

	/// <summary>
	/// Planner Assignment
	/// </summary>
	public PlannerAssignment PlannerAssignment
	{
		get => plannerAssignment ?? throw new NavigationUnloadedException();
		set => plannerAssignment = value;
	}
	private PlannerAssignment? plannerAssignment;

	// DO NOT USE 'VIRTUAL' FOR EF ENTITIES WITHOUT SETTING UP DB FOR ENTITY INHERITANCE
	// I DON'T SEE A CLASS INHERITING THIS ONE SO I'VE REMOVED IT

	/// <summary>
	/// Submissions
	/// </summary>
	public HashSet<PlannerSubmission> Submissions { get; set; } = [];

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<PlannerDueDate> builder)
	{
		/* See previous comments
		builder.HasKey(pdd => pdd.Id);
		builder.Property(pdd => pdd.PlannerAssignmentId).IsRequired();
		builder.Property(pdd => pdd.DueDateTime).IsRequired();
		builder.Property(pdd => pdd.Status).IsRequired();
		builder.Property(pdd => pdd.CreatedOn).IsRequired();
		builder.Property(pdd => pdd.UpdatedOn).IsRequired();
		builder.Property(pdd => pdd.Notes).HasMaxLength(500);
		builder.Property(pdd => pdd.RequiresSafetyCompliance);
		*/

		// Doubly defined this relationship, both will be in effect and will cause unexpected issues
		// I've commented out the one on the planner assignment side
		builder
			.HasOne(pdd => pdd.PlannerAssignment)
			.WithMany(pa => pa.PlannerDueDates)
			.HasForeignKey(pdd => pdd.PlannerAssignmentId)
			.OnDelete(DeleteBehavior.Cascade);

		/* Define these on the child entities for clarity
		builder.HasMany(pdd => pdd.Submissions)
			   .WithOne(ps => ps.PlannerDueDate)
			   .HasForeignKey(ps => ps.PlannerDueDateId)
			   .OnDelete(DeleteBehavior.SetNull);
		*/
	}
}
