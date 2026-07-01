using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Legacy.Data.Entities.Users;

namespace Platform.Legacy.Data.Entities.AccessControlLists;

/// <summary>
/// The effective users in the ACl that were calculated
/// </summary>
public class AccessControlListEffectiveUser : IEntityTypeConfiguration<AccessControlListEffectiveUser>
{
	/// <inheritdoc />
	public class Errors : EntityErrorCodeProvider<AccessControlListEffectiveUser> { }

	/// <summary>
	/// The ID of this entity
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
	/// The Owning ACL
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public AccessControlList AccessControlList
	{
		get => _accessControlList ?? throw new NavigationUnloadedException();
		set => _accessControlList = value;
	}

	private User? _user;

	/// <summary>
	/// The Referenced User
	/// </summary>
	/// <exception cref="NavigationUnloadedException"></exception>
	public User User
	{
		get => _user ?? throw new NavigationUnloadedException();
		set => _user = value;
	}

	/// <inheritdoc />
	public void Configure(EntityTypeBuilder<AccessControlListEffectiveUser> builder) { }
}
