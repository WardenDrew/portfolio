using Microsoft.AspNetCore.Identity;
using Sodium;

namespace Platform.Legacy.Core.Extensions;

public class Argon2PasswordHasher<TUser>(PasswordHash.StrengthArgon strength = PasswordHash.StrengthArgon.Interactive)
	: PasswordHasher<TUser>
	where TUser : class
{
	public override string HashPassword(TUser user, string password)
	{
		if (string.IsNullOrWhiteSpace(password))
		{
			throw new ArgumentNullException(nameof(password));
		}

		return PasswordHash.ArgonHashString(password: password, limit: strength);
	}

	public override PasswordVerificationResult VerifyHashedPassword(
		TUser user,
		string hashedPassword,
		string providedPassword
	)
	{
		if (string.IsNullOrWhiteSpace(hashedPassword))
		{
			throw new ArgumentNullException(nameof(hashedPassword));
		}

		if (string.IsNullOrWhiteSpace(providedPassword))
		{
			throw new ArgumentNullException(nameof(providedPassword));
		}

		// If we exception out, then return the password as failed.
		try
		{
			// Test with Argon 2
			bool validArgon2 = PasswordHash.ArgonHashStringVerify(hash: hashedPassword, password: providedPassword);

			if (validArgon2 && PasswordHash.ArgonPasswordNeedsRehash(hash: hashedPassword, limit: strength))
			{
				return PasswordVerificationResult.SuccessRehashNeeded;
			}

			if (validArgon2)
			{
				return PasswordVerificationResult.Success;
			}

			// Test with Default Identity (PBKDF2) and rehash if needed.
			PasswordVerificationResult validDefaultIdentity = base.VerifyHashedPassword(
				user: user,
				hashedPassword: hashedPassword,
				providedPassword: providedPassword
			);

			if (
				validDefaultIdentity == PasswordVerificationResult.Success
				|| validDefaultIdentity == PasswordVerificationResult.SuccessRehashNeeded
			)
			{
				return PasswordVerificationResult.SuccessRehashNeeded;
			}
		}
		catch (Exception)
		{
			return PasswordVerificationResult.Failed;
		}

		return PasswordVerificationResult.Failed;
	}
}
