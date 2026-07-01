using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Users;

// ReSharper disable EntityFramework.ModelValidation.CircularDependency

namespace Platform.Legacy.Data.Entities.AccessControlLists;

/// <summary>
/// A user directly in an ACL
/// </summary>
public class AccessControlListUser : IEntityTypeConfiguration<AccessControlListUser>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<AccessControlListUser> { }

	/// <summary>
	/// The Id of this record
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// The owning ACL
	/// </summary>
	public int AccessControlListId { get; set; }

	/// <summary>
	/// The referenced User
	/// </summary>
	public int UserId { get; set; }

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

	private User? _user;

	/// <summary>
	/// The referenced User
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public User User
	{
		get => _user ?? throw new NavigationUnloadedException();
		set => _user = value;
	}

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<AccessControlListUser> builder)
	{
		_ = builder.ToTable("AccessControlListUser");

		_ = builder
			.HasOne(e => e.AccessControlList)
			.WithMany(e => e.AccessControlListUsers)
			.HasForeignKey(e => e.AccessControlListId)
			.OnDelete(DeleteBehavior.Cascade);

		_ = builder
			.HasOne(e => e.User)
			.WithMany(e => e.AccessControlListUsers)
			.HasForeignKey(e => e.UserId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
