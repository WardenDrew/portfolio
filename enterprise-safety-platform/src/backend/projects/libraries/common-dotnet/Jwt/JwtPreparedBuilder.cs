using JWT.Builder;

namespace Platform.Common.Jwt;

/// <summary>
/// 
/// </summary>
public class JwtPreparedBuilder
{
	/// <summary>
	/// 
	/// </summary>
	public required string Algorithm { get; init; }
	/// <summary>
	/// 
	/// </summary>
	public required string KeyId { get; init; }
	/// <summary>
	/// 
	/// </summary>
	public required JwtBuilder Builder { get; init; }
}
