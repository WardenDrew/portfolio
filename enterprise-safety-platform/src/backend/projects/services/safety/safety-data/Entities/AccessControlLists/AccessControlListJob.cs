using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Companies;

// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.AccessControlLists;

/// <summary>
/// A job in an ACL
/// </summary>
public class AccessControlListJob : IEntityTypeConfiguration<AccessControlListJob>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<AccessControlListJob> { }

	/// <summary>
	/// The ID of this record
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// The owning ACL
	/// </summary>
	public int AccessControlListId { get; set; }

	/// <summary>
	/// The Referenced Job
	/// </summary>
	public int JobId { get; set; }

	private AccessControlList? _accessControlList;

	/// <summary>
	/// The owning ACL
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public AccessControlList AccessControlList
	{
		get => _accessControlList ?? throw new NavigationUnloadedException();
		set => _accessControlList = value;
	}

	private Job? _job;

	/// <summary>
	/// The referenced job
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public Job Job
	{
		get => _job ?? throw new NavigationUnloadedException();
		set => _job = value;
	}

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<AccessControlListJob> builder)
	{
		_ = builder.ToTable("AccessControlListJob");

		_ = builder
			.HasOne(e => e.AccessControlList)
			.WithMany(e => e.AccessControlListJobs)
			.HasForeignKey(e => e.AccessControlListId)
			.OnDelete(DeleteBehavior.Cascade);

		_ = builder
			.HasOne(e => e.Job)
			.WithMany(e => e.AccessControlListJobs)
			.HasForeignKey(e => e.JobId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
