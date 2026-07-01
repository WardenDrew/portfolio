using Platform.Legacy.Data.Entities.AccessControlLists;

namespace Platform.Legacy.Data.EntityInterfaces;

/// <summary>
/// An Entity that has an ACL
/// </summary>
public interface IHasAclEntity
{
	/// <summary>
	/// The ACL
	/// </summary>
	int AccessControlListId { get; set; }

	/// <summary>
	/// The ACL
	/// </summary>
	AccessControlList AccessControlList { get; set; }
}
