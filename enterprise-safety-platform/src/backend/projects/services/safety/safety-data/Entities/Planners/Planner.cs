using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Users;

// ReSharper disable EntityFramework.ModelValidation.CircularDependency

// Namespace is not unique per class, set namespace to match the folder the class resides in
namespace Platform.Legacy.Data.Entities.Planners;

/*
 * KEEP BUSINES LOGIC SEPERATE FROM DATABASE ENTITIES
 *
 * BUSINESS LOGIC MOVED TO Platform.Legacy.Core.Services.PlannerService
*/

/// <summary>
/// Planner
/// </summary>
public class Planner : IEntityTypeConfiguration<Planner>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<Planner> { }

	/// <summary>
	/// Record Id
	/// </summary>
	public int Id { get; init; } // use init only property for primary key

	/// <summary>
	/// Name of the planner
	/// </summary>
	[MaxLength(255)]
	public required string Name { get; set; }

	/// <summary>
	/// Description of Planner
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? Description { get; set; }

	/// <summary>
	/// Type of planner
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? Type { get; set; } = "safety";

	/// <summary>
	/// Company
	/// </summary>
	public int? CompanyId { get; set; }

	/// <summary>
	/// Created On
	/// </summary>
	public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Last Updated On
	/// </summary>
	public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// JSON Schema
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? SchemaJson { get; set; }

	/// <summary>
	/// JSON Metadata
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? MetaData { get; set; }

	/// <summary>
	/// Search Tags
	/// </summary>
	// ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
	public string? Tags { get; set; }

	/// <summary>
	/// Planner Status
	/// WARNING THIS IS AN ENUM, CHANGING ENUM ENTRY ORDER IN CODE WILL BREAK DB
	/// </summary>
	public PlannerStatus Status { get; set; } = PlannerStatus.Active;

	/// <summary>
	/// Created By User
	/// </summary>
	public int CreatedById { get; set; }

	/// <summary>
	/// Created By User
	/// </summary>
	public User CreatedBy
	{
		get => createdBy ?? throw new NavigationUnloadedException();
		set => createdBy = value;
	}
	private User? createdBy;

	/// <summary>
	/// Last Updated By user
	/// </summary>
	public int UpdatedById { get; set; }

	/// <summary>
	/// Last Access On
	/// </summary>
	public DateTime? LastAccessedOn { get; set; }

	/// <summary>
	/// Planner Assignments
	/// </summary>
	public HashSet<PlannerAssignment> PlannerAssignments { get; set; } = [];

	/// <summary>
	/// Planner Submissions
	/// </summary>
	public ICollection<PlannerSubmission> Submissions { get; set; } = [];

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<Planner> builder)
	{
		// EF Convention does not need to be specified
		//builder.HasKey(p => p.Id);

		// EF respects nullability and the MaxLength attribute
		//builder.Property(p => p.Name).IsRequired().HasMaxLength(255);

		// Default
		//builder.Property(p => p.Description).HasColumnType("text");

		/* see above
		builder.Property(p => p.CreatedOn).IsRequired();
		builder.Property(p => p.UpdatedOn).IsRequired();
		builder.Property(p => p.SchemaJson).HasColumnType("text");
		builder.Property(p => p.MetaData).HasColumnType("text");
		builder.Property(p => p.Tags).HasColumnType("text");
		builder.Property(p => p.Status).IsRequired();
		builder.Property(p => p.LastAccessedOn);
		*/

		// Explicit navigation relationships are helpful still in a project this size

		// If a user is deleted, this will break as CreatedById is a required field
		builder
			.HasOne(p => p.CreatedBy)
			.WithMany(x => x.Planners) // Define the other side of the relationship
			.HasForeignKey(p => p.CreatedById)
			.OnDelete(DeleteBehavior.Restrict); // Changed to restrict deletion of users if they have any planners still

		/* Prefer defining the child relationships to this object on the children
		builder.HasMany(p => p.PlannerAssignments)
			.WithOne(pa => pa.Planner)
			.HasForeignKey(pa => pa.PlannerId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(p => p.Submissions)
			.WithOne(ps => ps.Planner)
			.HasForeignKey(ps => ps.PlannerId)
			.OnDelete(DeleteBehavior.Restrict);
		*/
	}
}
