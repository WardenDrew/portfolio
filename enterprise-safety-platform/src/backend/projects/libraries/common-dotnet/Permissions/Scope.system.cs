namespace Platform.Common.Permissions;

public partial class Scope
{
	/// <summary>
	/// Parent scope for full control of all system information. Equivalent to old Operator/Developer roles
	/// </summary>
	public static readonly Scope SYSTEM = Scope.Create("system")
		.AddReadOnlySuffix()
		.Build();

	/// <summary>
	/// Scope for interacting with all users in the system, regardless of organization affiliation
	/// </summary>
	public static readonly Scope SYSTEM_USERS = Scope.SYSTEM.Child("user").Build();
	
	/// <summary>
	/// Scope for interacting with all organizations in the system, regardless of organization affiliation
	/// </summary>
	public static readonly Scope SYSTEM_ORGS = Scope.SYSTEM.Child("orgs").Build();

	/// <summary>
	/// Video Streaming/Uploading/Management
	/// </summary>
	public static readonly Scope SYSTEM_VIDEO = Scope.SYSTEM
		.Child("video")
		.ClearSuffixes()
		.Build();

	/// <summary>
	/// Go LIVE as the System
	/// </summary>
	public static readonly Scope SYSTEM_VIDEO_LIVE = Scope.SYSTEM_VIDEO.Child("live").Build();
	
	/// <summary>
	/// Manage System VOD Library (Upload/Delete/etc)
	/// </summary>
	public static readonly Scope SYSTEM_VIDEO_VOD = Scope.SYSTEM_VIDEO.Child("vod").Build();
	
	/// <summary>
	/// Moderate LIVEs and VODs from Organizations
	/// </summary>
	public static readonly Scope SYSTEM_VIDEO_MODERATE = Scope.SYSTEM_VIDEO.Child("moderate").Build();
}