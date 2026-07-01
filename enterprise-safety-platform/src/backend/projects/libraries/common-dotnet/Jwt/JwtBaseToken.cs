using System.Text.Json.Serialization;

namespace Platform.Common.Jwt;

/// <summary>
/// A Base JWT Token with the minimum fields to be useful in 99% of cases
/// Technically a token can have any body format, however these fields are useful
/// almost every time
/// </summary>
public class JwtBaseToken
{
	/// <summary>
    /// The "iss" (issuer) claim identifies the principal that issued the JWT.
	/// <see href="https://www.rfc-editor.org/rfc/rfc7519#section-4.1.1"/>
    /// </summary>
    [JsonPropertyName("iss")]
    public required string Issuer { get; set; }
    
    /// <summary>
    /// The "sub" (subject) claim identifies the principal that is the subject of the JWT. THe claims in a JWT are
    /// normally statements about the subject. The subject value MUST either be scoped to be locally unique in the
    /// context of the issuer or be globally unique.
	/// <see href="https://www.rfc-editor.org/rfc/rfc7519#section-4.1.2"/>
	/// </summary>
	/// <remarks>
	/// In cases of access tokens obtained through grants where a resource owner is involved, such as the authorization
	/// code grant, the value of "sub" SHOULD correspond to the subject identifier of the resource owner. In cases of
	/// access tokens obtained through grants where no resource owner is involved, such as the client credentials grant,
	/// the value of "sub" SHOULD correspond to an identifier the authorization server uses to indicate the client
	/// application. See Section 5 for more details on this scenario. Also, see Section 6 for a discussion about how
	/// different choices in assigning "sub" values can impact privacy.
	/// <see href="https://datatracker.ietf.org/doc/html/rfc9068#name-data-structure"/>
    /// </remarks>
    [JsonPropertyName("sub")]
	public required string Subject { get; set; }
    
    /// <summary>
    /// The "aud" (audience) claim identifies the recipients that the JWT is intended for. Each principal intended to
    /// process the JWT MUST identify itself with a value in the audience claim. If the principal processing the claim
    /// does not identify itself with a value in the "aud" claim when the claim is present, then the JWT MUST be
    /// rejected. In the general case, the "aud" value is an array of case-sensitive strings, each containing a
    /// StringOrURI value.
	/// <see href="https://www.rfc-editor.org/rfc/rfc7519#section-4.1.3"/>
	/// </summary>
	/// <remarks>
	/// See Section 3 for indications on how an authorization server should determine the value of "aud" depending on
	/// the request.
	/// <see href="https://datatracker.ietf.org/doc/html/rfc9068#name-data-structure"/>
    /// </remarks>
    [JsonPropertyName("aud")]
	public required string Audiences { get; set; }
    
    /// <summary>
    /// The "exp" (expiration time) claim identifies the expiration time on or after which the JWT MUST NOT be accepted
    /// for processing. The processing of the "exp" claim requires that the current date/time MUST be before the
    /// expiration date/time listed in the "exp" claim. Implementors MAY provide for some small leeway, usually no more
    /// than a few minutes, to account for clock skew. Its value MUST be a number containing a NumericDate value.
    /// <see href="https://www.rfc-editor.org/rfc/rfc7519#section-4.1.4"/>
    /// </summary>
    [JsonPropertyName("exp")]
    public required long ExpiresAt { get; set; }

	/// <summary>
	/// The "nbf" (not before) claim identifies the time before which the JWT MUST NOT be accepted for processing. The
	/// processing of the "nbf" claim requires that the current date/time MUST be after or equal to the not-before
	/// date/time listed in the "nbf" claim. Implementers MAY provide for some small leeway, usually no more than a few
	/// minutes, to account for clock skew. Its value MUST be a number containing a NumericDate value.
	/// <see href="https://www.rfc-editor.org/rfc/rfc7519#section-4.1.5"/>
	/// </summary>
	[JsonPropertyName("nbf")]
	public required long NotBeforeAt { get; set; }
	
	/// <summary>
	/// The "iat" (issued at) claim identifies the time at which the JWT was issued. This claim can be used to determine
	/// the age of the JWT. Its value MUST be a number containing a NumericDate value.
	/// <see href="https://www.rfc-editor.org/rfc/rfc7519#section-4.1.6"/>
	/// </summary>
	[JsonPropertyName("iat")]
	public required long IssuedAt { get; set; }
}
