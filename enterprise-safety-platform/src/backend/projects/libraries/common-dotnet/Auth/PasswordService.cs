using System;
using System.Diagnostics.CodeAnalysis;
using Sodium;

namespace Platform.Common.Auth;

/// <summary>
/// Password Hashing and Verification Service
/// </summary>
public class PasswordService
{
	private const PasswordHash.StrengthArgon STRENGTH = PasswordHash.StrengthArgon.Interactive;

	/// <summary>
	/// Hash Password
	/// </summary>
	/// <param name="password"></param>
	/// <returns></returns>
	[SuppressMessage(category: "Performance", checkId: "CA1822:Mark members as static")]
	public string HashPassword(string password)
	{
		return Sodium.PasswordHash.ArgonHashString(password: password, limit: PasswordService.STRENGTH);
	}

	/// <summary>
	/// Verify Password Hash
	/// </summary>
	/// <param name="hash"></param>
	/// <param name="password"></param>
	/// <returns></returns>
	public PasswordVerificationResult VerifyPasswordHash(string hash, string password)
	{
		try
		{
			// Test with Argon 2
			bool validArgon2 = PasswordHash.ArgonHashStringVerify(hash: hash, password: password);

			if (validArgon2 && PasswordHash.ArgonPasswordNeedsRehash(hash: hash, limit: PasswordService.STRENGTH))
			{
				return PasswordVerificationResult.SUCCESS_NEEDS_REHASH;
			}

			if (validArgon2)
			{
				return PasswordVerificationResult.SUCCESS;
			}
		}
		catch (Exception)
		{
			return PasswordVerificationResult.FAILURE;
		}

		return PasswordVerificationResult.FAILURE;
	}
}