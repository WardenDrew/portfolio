using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Companies;

// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.AccessControlLists;

/// <summary>
/// A group within an ACL
/// </summary>
public class AccessControlListGroup : IEntityTypeConfiguration<AccessControlListGroup>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<AccessControlListGroup> { }

	/// <summary>
	/// Id of this record
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// The owning ACL
	/// </summary>
	public int AccessControlListId { get; set; }

	/// <summary>
	/// The referenced Group
	/// </summary>
	public int GroupId { get; set; }

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

	private Group? _group;

	/// <summary>
	/// The reference Group
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public Group Group
	{
		get => _group ?? throw new NavigationUnloadedException();
		set => _group = value;
	}

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<AccessControlListGroup> builder)
	{
		_ = builder.ToTable("AccessControlListGroup");

		_ = builder
			.HasOne(e => e.AccessControlList)
			.WithMany(e => e.AccessControlListGroups)
			.HasForeignKey(e => e.AccessControlListId)
			.OnDelete(DeleteBehavior.Cascade);

		_ = builder
			.HasOne(e => e.Group)
			.WithMany(e => e.AccessControlListGroups)
			.HasForeignKey(e => e.GroupId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
