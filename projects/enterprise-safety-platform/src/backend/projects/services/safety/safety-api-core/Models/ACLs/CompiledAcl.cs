namespace Platform.Legacy.Core.Models.ACLs;

public class CompiledAcl
{
	public int Id { get; set; }
	public bool DefaultAllow { get; set; }
	public ICollection<int> UserIds { get; set; } = new HashSet<int>();
	public Guid CacheStamp { get; set; }

	public bool Allowed(int userId)
	{
		// Calc this once instead of twice
		bool containsUser = UserIds.Contains(userId);

		// Do we pass the ACL
		if (DefaultAllow && containsUser)
		{
			return false;
		} // Allow everyone who isn't on the list, and we are on the list
		if (!DefaultAllow && !containsUser)
		{
			return false;
		} // Allow only who is on the list, and we're not on the list

		return true;
	}
}
