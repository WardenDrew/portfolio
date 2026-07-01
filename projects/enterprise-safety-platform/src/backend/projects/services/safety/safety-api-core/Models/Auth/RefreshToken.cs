using System.Security.Claims;
using Platform.Common.Auth;
using Platform.Common.Configuration;

namespace Platform.Legacy.Core.Models.Auth;

#pragma warning disable CS0618 // Type or member is obsolete
public class RefreshToken : LegacyPlatformRefreshToken
#pragma warning restore CS0618 // Type or member is obsolete
{
	/*public static RefreshToken New(SettingsService settings)
	{
		RefreshToken token = new()
		{
			RefreshTokenGuid = Guid.NewGuid(),
			ExpiresOn = DateTime.UtcNow.AddSeconds(settings.Auth?.RefreshTokenLifetime ?? 2592000),
		};

		return token;
	}

	public static RefreshToken? FromClaimsPrincipal(ClaimsPrincipal? principal)
	{
		if (principal is null)
		{
			return null;
		}

		RefreshToken token = new();

		string? stringRefreshTokenGuid = principal.FindFirstValue(nameof(RefreshToken.RefreshTokenGuid).CamelCase());
		if (!Guid.TryParse(input: stringRefreshTokenGuid, result: out Guid refreshTokenGuid))
		{
			return null;
		}
		token.RefreshTokenGuid = refreshTokenGuid;

		string? stringExpiresOn = principal.FindFirstValue(nameof(RefreshToken.ExpiresOn).CamelCase());
		if (!long.TryParse(s: stringExpiresOn, result: out long longExpiresOn))
		{
			return null;
		}
		token.ExpiresOn = DateTime.FromBinary(longExpiresOn);

		return token;
	}*/

	/*public static RefreshToken? FromJwt(string token, JwtHelperService jwtHelperService)
	{
		return RefreshToken.FromClaimsPrincipal(jwtHelperService.ParseToken(token));
	}

	public string ToJwt(JwtHelperService jwtHelperService, ApiConfiguration apiConfiguration)
	{
		List<Claim> claims =
		[
			new(type: nameof(RefreshToken.RefreshTokenGuid).CamelCase(),
				value: this.RefreshTokenGuid.ToString("N").ToLowerInvariant()),
			new(type: nameof(RefreshToken.ExpiresOn).CamelCase(), value: this.ExpiresOn.ToBinary().ToString()),
		];

		DateTime notBefore = DateTime.UtcNow.AddMinutes(apiConfiguration.TokensAccessTokenNotBeforeGraceMinutes);

		return jwtHelperService.BuildToken(claims: claims, notBefore: notBefore, expires: this.ExpiresOn);
	}*/
}
