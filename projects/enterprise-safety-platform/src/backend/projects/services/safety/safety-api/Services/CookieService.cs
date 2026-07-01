using Platform.Common.Configuration;
using Platform.Legacy.Core.Services;

namespace Platform.Legacy.Api.Services;

internal class CookieService(IHttpContextAccessor httpContextAccessor)
{
	public void SetAuthCookies(
		string? accessTokenJwt,
		string? refreshTokenJwt,
		DateTime accessTokenExpiresAt,
		DateTime refreshTokenExpiresAt
	)
	{
		HttpContext? context = httpContextAccessor.HttpContext;

		if (accessTokenJwt != null)
		{
			context?.Response.Cookies.Append(
				key: "accessToken",
				value: accessTokenJwt,
				options: new CookieOptions
				{
					Domain = context.Request.Host.Value,
					Expires = new DateTimeOffset(accessTokenExpiresAt),
					IsEssential = true,
					HttpOnly = true,
					SameSite = SameSiteMode.None,
					Secure = true,
				}
			);
		}

		if (refreshTokenJwt != null)
		{
			context?.Response.Cookies.Append(
				key: "refreshToken",
				value: refreshTokenJwt,
				options: new CookieOptions
				{
					Domain = context.Request.Host.Value,
					Expires = new DateTimeOffset(refreshTokenExpiresAt),
					IsEssential = true,
					HttpOnly = true,
					SameSite = SameSiteMode.None,
					Secure = true,
				}
			);
		}
	}

	public void SetAuthCookies(BandaidDataModels.BandaidTokenResponse tokenResponse)
	{
		SetAuthCookies(
			accessTokenJwt: tokenResponse.AccessToken.Token,
			refreshTokenJwt: tokenResponse.RefreshToken.Token,
			accessTokenExpiresAt: tokenResponse.AccessToken.ExpiresUtc,
			refreshTokenExpiresAt: tokenResponse.RefreshToken.ExpiresUtc
		);
	}

	public void UnsetAuthCookies()
	{
		HttpContext? context = httpContextAccessor.HttpContext;

		context?.Response.Cookies.Append(
			key: "accessToken",
			value: "unset",
			options: new CookieOptions
			{
				Domain = context.Request.Host.Value,
				Expires = DateTimeOffset.MinValue,
				IsEssential = true,
				HttpOnly = true,
				SameSite = SameSiteMode.None,
				Secure = true,
			}
		);

		context?.Response.Cookies.Append(
			key: "refreshToken",
			value: "unset",
			options: new CookieOptions
			{
				Domain = context.Request.Host.Value,
				Expires = DateTimeOffset.MinValue,
				IsEssential = true,
				HttpOnly = true,
				SameSite = SameSiteMode.None,
				Secure = true,
			}
		);
	}
}
