using System;
using System.Text.Json.Serialization;
using Platform.Common.Json;

namespace Platform.Common.Auth;


/// <summary>
/// The Platform specific Access Token fields that are not well-known industry claims
/// This Type derives from <see cref="OidcAccessToken"/>
/// </summary>
public class PlatformAccessToken : OidcAccessToken
{
	/// <summary>
	/// The Organization ID the user is currently intendending to access. Can be null if they have not selected an
	/// organization yet for this session
	/// </summary>
	[JsonPropertyName("org_id")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required int? OrgId { get; set; }
	
	/// <summary>
	/// The Organization Name the user is currently intendending to access. Can be null if they have not selected an
	/// organization yet for this session
	/// </summary>
	[JsonPropertyName("org_name")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? OrgName { get; set; }
	
	/// <summary>
	/// The Organization User ID the user is using to access the given Organization
	/// </summary>
	[JsonPropertyName("org_user_id")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required int? OrgUserId { get; set; }

	/// <summary>
	/// ID of the Refresh Token used to issue this access token
	/// </summary>
	[JsonPropertyName("refresh_token_jti")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required Guid? RefreshTokenJwtId { get; set; }
}