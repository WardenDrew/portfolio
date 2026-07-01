using System.Collections.Generic;
using System.Text.Json.Serialization;
using NodaTime;
using Platform.Common.Json;

namespace Platform.Common.Auth;

/// <summary>
/// An OAuth 2.0 + OpenID Connect access token encoded in JWT format.
/// This Type derives from <see cref="OAuthBaseToken"/>
/// </summary>
public class OidcAccessToken : OAuthBaseToken
{
	/// <summary>
	/// End-User's full name in displayable form including all name parts, possibly including titles and suffixes,
	/// ordered according to the End-User's locale and preferences.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("name")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? Name { get; set; }

	/// <summary>
	/// URL of the End-User's profile picture. This URL MUST refer to an image file (for example, a PNG, JPEG, or GIF
	/// image file), rather than to a Web page containing an image. Note that this URL SHOULD specifically reference a
	/// profile photo of the End-User suitable for displaying when describing the End-User, rather than an arbitrary
	/// photo taken by the End-User.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("picture")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? Picture { get; set; }

	/// <summary>
	/// End-User's preferred e-mail address. Its value MUST conform to the RFC 5322 [RFC5322] addr-spec syntax. The RP
	/// MUST NOT rely upon this value being unique, as discussed in Section 5.7.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("email")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? Email { get; set; }

	/// <summary>
	/// True if the End-User's e-mail address has been verified; otherwise false. When this Claim Value is true, this
	/// means that the OP took affirmative steps to ensure that this e-mail address was controlled by the End-User at
	/// the time the verification was performed. The means by which an e-mail address is verified is context specific,
	/// and dependent upon the trust framework or contractual agreements within which the parties are operating.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("email_verified")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required bool? EmailVerified { get; set; }

	/// <summary>
	/// String from IANA Time Zone Database [IANA.time‑zones] representing the End-User's time zone. For example,
	/// Europe/Paris or America/Los_Angeles.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("zoneinfo")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? ZoneInfo { get; set; }

	// ReSharper disable once GrammarMistakeInComment This is verbatim from the openid spec
	/// <summary>
	/// End-User's locale, represented as a BCP47 [RFC5646] language tag. This is typically an ISO 639 Alpha-2 [ISO639]
	/// language code in lowercase and an ISO 3166-1 Alpha-2 [ISO3166‑1] country code in uppercase, separated by a dash.
	/// For example, en-US or fr-CA. As a compatibility note, some implementations have used an underscore as the
	/// separator rather than a dash, for example, en_US; Relying Parties MAY choose to accept this locale syntax as
	/// well.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("locale")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? Locale { get; set; }
	
	/// <summary>
	/// End-User's preferred telephone number. E.164 [E.164] is RECOMMENDED as the format of this Claim, for example,
	/// +1 (425) 555-1212 or +56 (2) 687 2400. If the phone number contains an extension, it is RECOMMENDED that the
	/// extension be represented using the RFC 3966 [RFC3966] extension syntax, for example, +1 (604) 555-1234;ext=5678.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("phone_number")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? PhoneNumber { get; set; }
	
	/// <summary>
	/// True if the End-User's phone number has been verified; otherwise false. When this Claim Value is true, this
	/// means that the OP took affirmative steps to ensure that this phone number was controlled by the End-User at the
	/// time the verification was performed. The means by which a phone number is verified is context specific, and
	/// dependent upon the trust framework or contractual agreements within which the parties are operating. When true,
	/// the phone_number Claim MUST be in E.164 format and any extensions MUST be represented in RFC 3966 format.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("phone_number_verified")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required bool? PhoneNumberVerified { get; set; }
	
	/// <summary>
	/// Time the End-User's information was last updated. Its value is a JSON number representing the number of seconds
	/// from 1970-01-01T00:00:00Z as measured in UTC until the date/time.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims"/>
	/// </summary>
	[JsonPropertyName("updated_at")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required long? UpdatedAt { get; set; }
	
	/// <summary>
	/// Time when the End-User authentication occurred. Its value is a JSON number representing the number of seconds
	/// from 1970-01-01T00:00:00Z as measured in UTC until the date/time. When a max_age request is made or when
	/// auth_time is requested as an Essential Claim, then this Claim is REQUIRED; otherwise, its inclusion is OPTIONAL.
	/// (The auth_time Claim semantically corresponds to the OpenID 2.0 PAPE [OpenID.PAPE] auth_time response
	/// parameter.)
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#IDToken"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// The claims listed in this section MAY be issued in the context of authorization grants involving the resource
	/// owner and reflect the types and strength of authentication in the access token that the authentication server
	/// enforced prior to returning the authorization response to the client. Their values are fixed and remain the same
	/// across all access tokens that derive from a given authorization response, whether the access token was obtained
	/// directly in the response (e.g., via the implicit flow) or after one or more token exchanges (e.g., obtaining a
	/// fresh access token using a refresh token or exchanging one access token for another via [RFC8693] procedures).
	/// <see href="https://datatracker.ietf.org/doc/html/rfc9068#section-2.2.1"/>
	/// </para>
	/// </remarks>
	[JsonPropertyName("auth_time")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required long? AuthenticationTime { get; set; }
    
	/// <summary>
	/// OPTIONAL. Authentication Context Class Reference. String specifying an Authentication Context Class Reference
	/// value that identifies the Authentication Context Class that the authentication performed satisfied. The value
	/// "0" indicates the End-User authentication did not meet the requirements of ISO/IEC 29115 [ISO29115] level 1.
	/// For historic reasons, the value "0" is used to indicate that there is no confidence that the same person is
	/// actually there. Authentications with level 0 SHOULD NOT be used to authorize access to any resource of any
	/// monetary value. (This corresponds to the OpenID 2.0 PAPE [OpenID.PAPE] nist_auth_level 0.) An absolute URI or
	/// an RFC 6711 [RFC6711] registered name SHOULD be used as the acr value; registered names MUST NOT be used with a
	/// different meaning than that which is registered. Parties using this claim will need to agree upon the meanings
	/// of the values used, which may be context specific. The acr value is a case-sensitive string.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#IDToken"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// The claims listed in this section MAY be issued in the context of authorization grants involving the resource
	/// owner and reflect the types and strength of authentication in the access token that the authentication server
	/// enforced prior to returning the authorization response to the client. Their values are fixed and remain the same
	/// across all access tokens that derive from a given authorization response, whether the access token was obtained
	/// directly in the response (e.g., via the implicit flow) or after one or more token exchanges (e.g., obtaining a
	/// fresh access token using a refresh token or exchanging one access token for another via [RFC8693] procedures).
	/// <see href="https://datatracker.ietf.org/doc/html/rfc9068#section-2.2.1"/>
	/// </para>
	/// </remarks>
	[JsonPropertyName("acr")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required string? AuthenticationContextClassReference { get; set; }
    
	/// <summary>
	/// OPTIONAL. Authentication Methods References. JSON array of strings that are identifiers for authentication
	/// methods used in the authentication. For instance, values might indicate that both password and OTP
	/// authentication methods were used. The amr value is an array of case-sensitive strings. Values used in the amr
	/// Claim SHOULD be from those registered in the IANA Authentication Method Reference Values registry [IANA.AMR]
	/// established by [RFC8176]; parties using this claim will need to agree upon the meanings of any unregistered
	/// values used, which may be context specific.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#IDToken"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// The claims listed in this section MAY be issued in the context of authorization grants involving the resource
	/// owner and reflect the types and strength of authentication in the access token that the authentication server
	/// enforced prior to returning the authorization response to the client. Their values are fixed and remain the same
	/// across all access tokens that derive from a given authorization response, whether the access token was obtained
	/// directly in the response (e.g., via the implicit flow) or after one or more token exchanges (e.g., obtaining a
	/// fresh access token using a refresh token or exchanging one access token for another via [RFC8693] procedures).
	/// <see href="https://datatracker.ietf.org/doc/html/rfc9068#section-2.2.1"/>
	/// </para>
	/// </remarks>
	[JsonPropertyName("amr")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required List<string>? AuthenticationMethodsReference { get; set; }

	/// <summary>
	/// String value used to associate a Client session with an ID Token, and to mitigate replay attacks. The value is
	/// passed through unmodified from the Authentication Request to the ID Token. If present in the ID Token, Clients
	/// MUST verify that the nonce Claim Value is equal to the value of the nonce parameter sent in the Authentication
	/// Request. If present in the Authentication Request, Authorization Servers MUST include a nonce Claim in the ID
	/// Token with the Claim Value being the nonce value sent in the Authentication Request. Authorization Servers
	/// SHOULD perform no other processing on nonce values used. The nonce value is a case-sensitive string.
	/// <see href="https://openid.net/specs/openid-connect-core-1_0.html#IDToken"/>
	/// </summary>
	[JsonPropertyName("nonce")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required string? Nonce { get; set; }
}