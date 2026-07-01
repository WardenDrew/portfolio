namespace Platform.Common.Permissions;

public partial class Scope
{
	/// <summary>
	/// Details about the subject user, except priveleged information
	/// </summary>
	public static readonly Scope USER = Scope.Create("user")
		.AddReadOnlySuffix()
		.Build();

	/// <summary>
	/// Privileged information about the subject user, such as authentication details, encryption keys,
	/// signing keys, etc.
	/// </summary>
	public static readonly Scope USER_PRIVILEGED = Scope.Create("user_privileged")
		.Privileged()
		.AddReadOnlySuffix()
		.Build();
}