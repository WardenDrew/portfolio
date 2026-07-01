using System.Collections.Generic;
using System.Text.Json.Serialization;
using Platform.Common.Json;

namespace Platform.Common.Auth;

/// <summary>
/// The Platform specific Refresh Token fields that are not well-known industry claims
/// This Type derives from <see cref="OAuthBaseToken"/>
/// </summary>
public class PlatformRefreshToken : OAuthBaseToken
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
	/// The Organization User ID the user is using to access the given Organization
	/// </summary>
	[JsonPropertyName("org_user_id")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required int? OrgUserId { get; set; }
	
	/// <summary>
	/// Auth time for OIDC
	/// </summary>
	[JsonPropertyName("auth_time")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required long? AuthenticationTime { get; set; }
	
	/// <summary>
	/// Auth Class Reference for OIDC
	/// </summary>
	[JsonPropertyName("acr")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? AuthenticationContextClassReference { get; set; }
	
	/// <summary>
	/// Auth method reference for OIDC
	/// </summary>
	[JsonPropertyName("amr")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required List<string>? AuthenticationMethodsReference { get; set; }
}