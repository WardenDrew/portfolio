namespace Platform.Common.Permissions;

public partial class Scope
{
	/// <summary>
	/// Parent scope for full control of all organization (Company) resources. Equivalent to old SuperAdmin role
	/// </summary>
	public static readonly Scope ORG = Scope.Create("org")
		.AddReadOnlySuffix()
		.Organization()
		.Assignable()
		.Build();

	/// <summary>
	/// Billing information about the organization
	/// </summary>
	public static readonly Scope ORG_BILLING = Scope.ORG.Child("billing").Build();

	/// <summary>
	/// Users connected to the organization, parent scope of all org/user related actions
	/// </summary>
	public static readonly Scope ORG_USERS = Scope.ORG.Child("users").Build();

	/// <summary>
	/// Invitations for bringing new users into the organization
	/// </summary>
	public static readonly Scope ORG_USERS_INVITE = Scope.ORG_USERS.Child("invite").Build();
	
	/// <summary>
	/// The Organization User profile for the subject user
	/// </summary>
	public static readonly Scope ORG_USERS_SELF = Scope.ORG_USERS.Child("self").Build();
	
	/// <summary>
	/// All timeclocks within the organization
	/// </summary>
	public static readonly Scope ORG_TIMECLOCKS = Scope.ORG.Child("timeclock").Build();
	
	/// <summary>
	/// The subject user's timeclocks within the organization
	/// </summary>
	public static readonly Scope ORG_TIMECLOCKS_SELF = Scope.ORG_TIMECLOCKS.Child("self").Build();

	/// <summary>
	/// Tags in the organization
	/// </summary>
	public static readonly Scope ORG_TAGS = Scope.ORG.Child("tag")
		.ClearSuffixes()
		.Build();
}