using System.Text.Json.Serialization;

namespace Platform.Common.Jwt;

/// <summary>
/// 
/// </summary>
public class JwtHeader
{
	/// <summary>
	/// Algorithm used
	/// </summary>
	[JsonPropertyName("alg")]
	public string? Algorithm { get; set; }
	
	/// <summary>
	/// Type of token
	/// </summary>
	[JsonPropertyName("typ")]
	public string? TokenType { get; set; }
	
	/// <summary>
	/// Key ID used for encryption/signing
	/// </summary>
	[JsonPropertyName("kid")]
	public string? KeyId { get; set; }
}
