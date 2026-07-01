namespace Platform.Common.Auth;

/// <summary>
/// Password hash verification result
/// </summary>
public enum PasswordVerificationResult
{
	/// <summary>
	/// The password matches the hash
	/// </summary>
	SUCCESS,
	/// <summary>
	/// The password matches the hash but the hash needs to be rehashed
	/// </summary>
	SUCCESS_NEEDS_REHASH,
	/// <summary>
	/// The password does not match the hash
	/// </summary>
	FAILURE,
}