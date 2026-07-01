namespace Platform.Common.Permissions;

public partial class Scope
{
	/// <summary>
	/// The scope denoting that this token can be used to request an access token with
	/// </summary>
	public static readonly Scope REFRESH_TOKEN = Scope.Build("refresh_token");
}