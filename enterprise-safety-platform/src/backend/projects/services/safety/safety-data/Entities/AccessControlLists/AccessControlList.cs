using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Companies;
using Platform.Legacy.Data.Entities.Documents;

// ReSharper disable EntityFramework.ModelValidation.CircularDependency
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Platform.Legacy.Data.Entities.AccessControlLists;

/// <summary>
/// Access Control list used to control access to documents
/// </summary>
public class AccessControlList : IEntityTypeConfiguration<AccessControlList>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<AccessControlList> { }

	/// <summary>
	/// Id of the ACL
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Company owning the ACL
	/// </summary>
	public int CompanyId { get; set; }

	/// <summary>
	/// If the ACl should just include the whole company instead of fine-grained
	/// </summary>
	public bool? AllCompany { get; set; }

	private Company? _company;

	/// <summary>
	/// Company owning the ACL
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public Company Company
	{
		get => _company ?? throw new NavigationUnloadedException();
		set => _company = value;
	}

	/// <summary>
	/// The users in the ACL
	/// </summary>
	public ICollection<AccessControlListUser> AccessControlListUsers { get; set; } =
		new HashSet<AccessControlListUser>();

	/// <summary>
	/// The groups in the ACL
	/// </summary>
	public ICollection<AccessControlListGroup> AccessControlListGroups { get; set; } =
		new HashSet<AccessControlListGroup>();

	/// <summary>
	/// The Jobs in the ACL
	/// </summary>
	public ICollection<AccessControlListJob> AccessControlListJobs { get; set; } = new HashSet<AccessControlListJob>();

	/// <summary>
	/// The calculated effective users in the ACL
	/// </summary>
	public ICollection<AccessControlListEffectiveUser> AccessControlListEffectiveUsers { get; set; } =
		new HashSet<AccessControlListEffectiveUser>();

	/// <summary>
	/// The documents protected by this ACL
	/// </summary>
	public ICollection<Document> Documents { get; set; } = new HashSet<Document>();

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<AccessControlList> builder)
	{
		_ = builder.ToTable("AccessControlList");

		_ = builder
			.HasOne(e => e.Company)
			.WithMany(e => e.AccessControlLists)
			.HasForeignKey(e => e.CompanyId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
