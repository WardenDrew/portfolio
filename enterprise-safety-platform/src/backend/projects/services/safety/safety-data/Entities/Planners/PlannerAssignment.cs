using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Users;

// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.Planners;

/// <summary>
/// Planner Assignment
/// </summary>
public class PlannerAssignment : IEntityTypeConfiguration<PlannerAssignment>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<PlannerAssignment> { }

	/// <summary>
	/// Record Id
	/// </summary>
	public int Id { get; init; }

	/// <summary>
	/// Planner
	/// </summary>
	public required int PlannerId { get; set; }

	/// <summary>
	/// Assignment Name
	/// </summary>
	[MaxLength(255)]
	public string? Name { get; set; }

	/// <summary>
	/// Assignment Description
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? Description { get; set; }

	/// <summary>
	/// Recurrence Type
	/// WARNING THIS IS AN ENUM, CHANGING ENUM ENTRY ORDER IN CODE WILL BREAK DB
	/// </summary>
	public PlannerRecurrenceType PlannerRecurrenceType { get; set; }

	/// <summary>
	/// Recurrence Interval
	/// </summary>
	public int? RecurrenceInterval { get; set; }

	/// <summary>
	/// Recurrence Custom Pattern
	/// </summary>
	[MaxLength(255)]
	public string? RecurrenceCustomPattern { get; set; }

	/// <summary>
	/// Planner JSON Schema (COPY?)
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? PlannerJson { get; set; }

	/// <summary>
	/// Assignment Created On
	/// </summary>
	public DateTime CreatedOn { get; set; }

	/// <summary>
	/// Assignment Updated On
	/// </summary>
	public DateTime UpdatedOn { get; set; }

	/// <summary>
	/// Assignment Status
	/// WARNING THIS IS AN ENUM, CHANGING ENUM ENTRY ORDER IN CODE WILL BREAK DB
	/// </summary>
	public PlannerAssignmentStatus Status { get; set; } = PlannerAssignmentStatus.Draft;

	/// <summary>
	/// Assignment Start Date
	/// </summary>
	public DateTime? StartDate { get; set; }

	/// <summary>
	/// Assignment End Date
	/// </summary>
	public DateTime? EndDate { get; set; }

	/// <summary>
	/// Assignment Priority
	/// </summary>
	public int Priority { get; set; }

	/// <summary>
	/// Assigned to user
	/// </summary>
	public int? AssignedToId { get; set; }

	/// <summary>
	/// Assigned by user
	/// </summary>
	public int? AssignedById { get; set; }

	/// <summary>
	/// Assign method
	/// </summary>
	[MaxLength(255)]
	public string? AssignedVia { get; set; }

	/// <summary>
	/// search tags
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? Tags { get; set; }

	/// <summary>
	/// completion percentage (out of 100?)
	/// </summary>
	public int CompletionPercentage { get; set; } // 0 IS THE DEFAULT FOR INT

	/// <summary>
	/// Last Occurence
	/// </summary>
	public DateTime? LastOccurrence { get; set; }

	/// <summary>
	/// Next Occurence
	/// </summary>
	public DateTime? NextOccurrence { get; set; }

	/// <summary>
	/// Requires Safety Compliance
	/// </summary>
	public bool RequiresSafetyCompliance { get; set; } // FALSE IS THE DEFEAULT FOR BOOL

	// Navigation properties

	/// <summary>
	/// The Planner
	/// </summary>
	public Planner Planner
	{
		get => planner ?? throw new NavigationUnloadedException();
		set => planner = value;
	}
	private Planner? planner;

	/// <summary>
	/// Assigned to user
	/// </summary>
	public User? AssignedTo { get; set; }

	/// <summary>
	/// Assigned by user
	/// </summary>
	public User? AssignedBy { get; set; }

	// DO NOT USE 'VIRTUAL' FOR EF ENTITIES WITHOUT SETTING UP DB FOR ENTITY INHERITANCE
	// I DON'T SEE A CLASS INHERITING THIS ONE SO I'VE REMOVED IT

	/// <summary>
	/// Due Dates
	/// </summary>
	public HashSet<PlannerDueDate> PlannerDueDates { get; set; } = [];

	/// <summary>
	/// Submissions
	/// </summary>
	public HashSet<PlannerSubmission> Submissions { get; set; } = [];

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<PlannerAssignment> builder)
	{
		/* EF Default convention
		builder.HasKey(pa => pa.Id);
		builder.Property(pa => pa.Id).ValueGeneratedOnAdd();
		*/

		/* See previous comments on Planner
		 * You don't need to call the property method unless changing something
		 * that can't be defined with attributes
		builder.Property(pa => pa.PlannerId).IsRequired();
		builder.Property(pa => pa.Name).HasMaxLength(255);
		builder.Property(pa => pa.Description).HasColumnType("text");
		builder.Property(pa => pa.RecurrenceCustomPattern).HasMaxLength(255);
		builder.Property(pa => pa.PlannerJson).HasColumnType("text");
		builder.Property(pa => pa.CreatedOn);
		builder.Property(pa => pa.UpdatedOn);
		builder.Property(pa => pa.Status);
		builder.Property(pa => pa.Priority);
		builder.Property(pa => pa.AssignedVia).HasMaxLength(255);
		builder.Property(pa => pa.Tags).HasColumnType("text");
		builder.Property(pa => pa.CompletionPercentage);
		builder.Property(pa => pa.RequiresSafetyCompliance);
		*/

		// Doubly defined this relationship, both will be in effect and will cause unexpected issues
		// I've commented out the one on the planner side
		builder
			.HasOne(pa => pa.Planner)
			.WithMany(p => p.PlannerAssignments)
			.HasForeignKey(pa => pa.PlannerId)
			.IsRequired()
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasOne(pa => pa.AssignedTo)
			.WithMany(x => x.AssignedToPlannerAssignments) // Define the other side of the relationship navigation
			.HasForeignKey(pa => pa.AssignedToId)
			.OnDelete(DeleteBehavior.SetNull);

		builder
			.HasOne(pa => pa.AssignedBy)
			.WithMany(x => x.AssignedByPlannerAssignments) // Define the other side of the relationship navigation
			.HasForeignKey(pa => pa.AssignedById)
			.OnDelete(DeleteBehavior.SetNull);

		/* Define these on the child entities for clarity
		builder.HasMany(pa => pa.PlannerDueDates)
			   .WithOne(pdd => pdd.PlannerAssignment)
			   .HasForeignKey(pdd => pdd.PlannerAssignmentId)
			   .OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(pa => pa.Submissions)
				.WithOne(ps => ps.PlannerAssignment)
				.HasForeignKey(ps => ps.PlannerAssignmentId)
				.OnDelete(DeleteBehavior.SetNull);
		*/
	}
}
