using System.Security.Claims;

namespace Platform.Legacy.Core.Models;

public class AssetToken
{
	public int AssetId { get; set; }
	public DateTime ExpiresOn { get; set; }

	public static AssetToken New(int assetId)
	{
		AssetToken token = new() { AssetId = assetId, ExpiresOn = DateTime.UtcNow.AddMinutes(5), };

		return token;
	}

	/*public static AssetToken? FromClaimsPrincipal(ClaimsPrincipal? principal)
	{
		if (principal is null)
		{
			return null;
		}

		AssetToken token = new();

		string? stringAssetId = principal.FindFirstValue(nameof(AssetToken.AssetId).CamelCase());
		if (!int.TryParse(s: stringAssetId, result: out int assetId))
		{
			return null;
		}
		token.AssetId = assetId;

		string? stringExpiresOn = principal.FindFirstValue(nameof(AssetToken.ExpiresOn).CamelCase());
		if (!long.TryParse(s: stringExpiresOn, result: out long longExpiresOn))
		{
			return null;
		}
		token.ExpiresOn = DateTime.FromBinary(longExpiresOn);

		return token;
	}

	public static AssetToken? FromJwt(string token, JwtHelperService jwtHelperService)
	{
		return AssetToken.FromClaimsPrincipal(jwtHelperService.ParseToken(token));
	}

	public string ToJwt(JwtHelperService jwtHelperService, ApiConfiguration apiConfiguration)
	{
		List<Claim> claims =
		[
			new(type: nameof(AssetToken.AssetId).CamelCase(), value: this.AssetId.ToString()),
			new(type: nameof(AssetToken.ExpiresOn).CamelCase(), value: this.ExpiresOn.ToBinary().ToString()),
		];

		DateTime notBefore = DateTime.UtcNow.AddMinutes(apiConfiguration.TokensAccessTokenNotBeforeGraceMinutes);

		return jwtHelperService.BuildToken(claims: claims, notBefore: notBefore, expires: this.ExpiresOn);
	}*/
}
