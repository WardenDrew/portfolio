using System;
using System.Text.Json.Serialization;
using NodaTime;
using Platform.Common.Json;
using Platform.Common.Jwt;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Platform.Common.Auth;

/// <summary>
/// The base from which the OAuth 2.0 access and refresh tokens are derived
/// </summary>
/// <remarks>
/// <para>
/// <see href="https://datatracker.ietf.org/doc/html/rfc9068#name-terminology"/>
/// </para>
/// <para>
/// <see href="https://datatracker.ietf.org/doc/html/rfc9068#name-data-structure"/>
/// </para>
/// </remarks>
public class OAuthBaseToken : JwtBaseToken
{
    /// <summary>
	/// The "jti" (JWT ID) claim provides a unique identifier for the JWT. The identifier value MUST be assigned in a
	/// manner that ensures that there is a negligible probability that the same value will be accidentally assigned to
	/// a different data object; if the application uses multiple issuers, collisions MUST be prevented among values
	/// produced by different issues as well. the "jti" claim can be used to prevent the JWT from being replayed. The
	/// "jti" value is a case-sensitive string.
	/// <see href="https://www.rfc-editor.org/rfc/rfc7519#section-4.1.7"/>
	/// </summary>
	[JsonPropertyName("jti")]
	public required Guid JwtId { get; set; }
	
	/// <inheritdoc cref="ActorClaim"/> 
	[JsonPropertyName("act")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public required ActorClaim? Actor { get; set; }
	
	/// <summary>
	/// The client_id claim carries the client identifier of the OAuth 2.0 [RFC6749] client that requested the token.
	/// <see href="https://datatracker.ietf.org/doc/html/rfc8693#section-4.3"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// The authorization server issues the registered client a client identifier -- a unique string representing the
	/// registration information provided by the client.  The client identifier is not a secret; it is exposed to the
	/// resource owner and MUST NOT be used alone for client authentication.  The client identifier is unique to the
	/// authorization server.
	/// <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-2.2"/>
	/// </para>
	/// </remarks>
	[JsonPropertyName("client_id")]
	public required Guid ClientId { get; set; }

	
	/// <summary>
	/// The value of the "scope" claim is a JSON string containing a space-separated list of scopes associated with the
	/// token, in the format described in Section 3.3 of [RFC6749].
	/// <see href="https://datatracker.ietf.org/doc/html/rfc8693#section-4.2"/>
	/// </summary>
	/// <remarks>
	/// <code>
	/// "scope": "email profile phone address"
	/// </code>
	/// </remarks>

	[JsonPropertyName("scope")]
	[JsonNotRequired]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	// ReSharper disable once InconsistentNaming
	public required string? Scopes { get; set; }
}