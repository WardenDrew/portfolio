using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Platform.Common.Auth;
using Platform.Common.Configuration;
using Platform.Common.Jwt;
using Platform.Common.Permissions;
using Platform.Legacy.Core.Enums;
using Platform.Legacy.Core.Extensions.ServiceScanning;
using Platform.Legacy.Core.Models.Auth;
using Platform.Legacy.Data.Entities.Companies;
using Platform.Legacy.Data.Entities.Users;

namespace Platform.Legacy.Core.Services;

public interface IAuthorizationService : IServiceScanningServiceInterface
{
	Task<TokenResponse> RegisterUserSession(User user, CancellationToken cancellationToken = default);
	AccessToken? ParseCurrentAccessToken();
	Task<IResponse> ExchangeRefreshToken(string token, bool keepOldSession = false, CancellationToken cancellationToken = default);
	Task<IResponse> BandaidExchangeRefreshToken(string token, bool keepOldSession = false, CancellationToken cancellationToken = default);
	Task<BandaidDataModels.BandaidTokenResponse> BandaidRegisterUserSession(
		User user,
		CancellationToken cancellationToken = default
	);

	bool OldCheckPermissions(
		Permissions permissions,
		[NotNullWhen(true)] out AccessToken? accessToken,
		out IResponse permissionCheckResponse
	);
}

public class BandaidDataModels
{
	public class BandaidAccessToken
	{
		public string? Token { get; set; }
		public DateTime IssuedUtc { get; set; }
		public DateTime ExpiresUtc { get; set; }
		public string? Issuer { get; set; }
		public int UserId { get; set; }
		public string? Name { get; set; }
	}

	public class BandaidRefreshToken
	{
		public string? Token { get; set; }
		public DateTime IssuedUtc { get; set; }
		public DateTime ExpiresUtc { get; set; }
	}

	public class BandaidTokenResponse
	{
		public BandaidAccessToken AccessToken { get; set; } = new();
		public BandaidRefreshToken RefreshToken { get; set; } = new();
	}
}

public class AuthorizationService(
	UserManager<User> userManager,
	LegacyDbContext db,
	IConfiguration configuration,
	JwtSerializer jwtSerializer,
	IHttpContextAccessor httpContextAccessor,
	ILogger<AuthorizationService> logger)
	: IAuthorizationService, IServiceScanningScopedImplementation
{
	private readonly AuthSettings? authSettings = configuration
		.GetSection(AuthSettings.CONFIGURATION_KEY)
		.Get<AuthSettings>();
	
	public readonly long DEFAULT_REFRESH_TOKEN_DURATION = 2592000;
	public readonly long DEFAULT_ACCESS_TOKEN_DURATION = 300;
	public readonly long DEFAULT_CLOCK_SKEW_GRACE = 60;

	private async Task<AccessToken> LegacyShimBuildAccessToken(
		User user,
		OAuthBaseToken baseToken,
		RefreshToken refreshToken)
	{
		// Calc expiration for access tokens
		Instant expires = Instant.Min(
			x: Instant.FromUnixTimeSeconds(baseToken.IssuedAt)
				.Plus(Duration.FromSeconds(authSettings?.AccessTokenLifetime ?? DEFAULT_ACCESS_TOKEN_DURATION)),
			y: Instant.FromUnixTimeSeconds(refreshToken.ExpiresAt));

		Instant lastUpdated = user.UpdatedOn.HasValue
			? Instant.FromDateTimeUtc(DateTime.SpecifyKind(value: user.UpdatedOn.Value, kind: DateTimeKind.Utc))
			: Instant.MinValue;

		// Calc extra perms
		bool isDeveloper = await userManager.IsInRoleAsync(
			user: user,
			role: Roles.DEVELOPER);

		bool isOperator = await userManager.IsInRoleAsync(
			user: user,
			role: Roles.OPERATOR);

		// Legacy Scopes transition
		List<Scope> scopes = [ Scope.USER, ];

		if (isDeveloper || isOperator)
		{
			scopes.Add(Scope.SYSTEM);
		}

		if (user.SuperAdmin)
		{
			scopes.Add(Scope.ORG);
		}
		else
		{
			if (user.BillingAdmin)
			{
				scopes.Add(Scope.ORG_BILLING);
			}

			if (user.UserAdmin)
			{
				scopes.Add(Scope.ORG_USERS);
				scopes.Add(Scope.ORG_TIMECLOCKS);
			}
		}

		string scopesString = string.Join(
			separator: " ",
			values: scopes.Select(x => x.ToString()));

		// Build the token model itself
		AccessToken token = new()
		{
			// OAuth Base Fields
			Issuer = baseToken.Issuer,
			Subject = baseToken.Subject,
			Audiences = baseToken.Audiences,
			ExpiresAt = expires.ToUnixTimeSeconds(),
			NotBeforeAt = baseToken.NotBeforeAt,
			IssuedAt = baseToken.IssuedAt,
			JwtId = baseToken.JwtId,
			Actor = baseToken.Actor,
			ClientId = baseToken.ClientId,

			// Overrides
			Scopes = scopesString,

			// OIDC Fields
			Name = user.FriendlyName,
			Picture = user.ProfileImageAsset?.PublicId != null
				? $"https://cdn.example.com/profiles/{user.ProfileImageAsset?.PublicId}"
				: null,
			Email = user.Email,
			EmailVerified = user.EmailConfirmed,
			ZoneInfo = null,
			Locale = null,
			PhoneNumber = user.PhoneNumber,
			PhoneNumberVerified = user.PhoneNumberConfirmed,
			UpdatedAt = lastUpdated.ToUnixTimeSeconds(),
			AuthenticationTime = refreshToken.AuthenticationTime,
			AuthenticationContextClassReference = refreshToken.AuthenticationContextClassReference,
			AuthenticationMethodsReference = refreshToken.AuthenticationMethodsReference,
			Nonce = Guid.NewGuid().ToString(),

			// Platform Access Token Fields
			OrgId = user.CompanyId,
			OrgName = user.Company.Name,
			OrgUserId = user.Id,
			RefreshTokenJwtId = refreshToken.JwtId,

			// Old Token - Was Manually set
			AccessTokenGuid = Guid.NewGuid(),
			ExpiresOn = expires.ToDateTimeUtc(),
			IsDeveloper = isDeveloper,
			IsOperator = isOperator,

			// Old Token - Was Automapper set
			UserId = user.Id,
			CompanyId = user.CompanyId,
			UserName = user.UserName,
			UserEmailAddress = user.Email,
			FriendlyName = user.FriendlyName,
			Initials = user.Initials,
			ProfileImageAssetId = user.ProfileImageAssetId,
			ProfileImageAssetKey = user.ProfileImageAsset?.PublicId ?? Guid.Empty,
			CompanyName = user.Company.Name,
			CultureLanguageCode = user.Culture?.OfficialLanguageCode,
			CultureCountryCode = user.Culture?.CountryCode,
			IsSuperAdmin = user.SuperAdmin,
			IsBillingAdmin = user.BillingAdmin,
			IsUserAdmin = user.UserAdmin,
		};

		return token;
	}

	private RefreshToken LegacyShimBuildRefreshToken(
		User user,
		OAuthBaseToken baseToken)
	{
		RefreshToken token = new()
		{
			// OAuth Base
			Issuer = baseToken.Issuer,
			Subject = baseToken.Subject,
			Audiences = baseToken.Audiences,
			ExpiresAt = baseToken.ExpiresAt,
			NotBeforeAt = baseToken.NotBeforeAt,
			IssuedAt = baseToken.IssuedAt,
			JwtId = baseToken.JwtId,
			Actor = baseToken.Actor,
			ClientId = baseToken.ClientId,

			// Override
			Scopes = $"{Scope.REFRESH_TOKEN}",

			// Platform Refresh Token
			OrgId = user.CompanyId,
			OrgUserId = user.Id,
			AuthenticationTime = baseToken.IssuedAt,
			AuthenticationContextClassReference = "urn:platform:loa1:pwd",
			AuthenticationMethodsReference = ["pwd",],

			// Old Token
			RefreshTokenGuid = Guid.NewGuid(),
			ExpiresOn = Instant.FromUnixTimeSeconds(baseToken.ExpiresAt).ToDateTimeUtc(),
		};

		return token;
	}

	private OAuthBaseToken LegacyShimBuildBaseToken(User user)
	{
		// Time Calcs
		Instant now = SystemClock.Instance.GetCurrentInstant();
		Instant expires = now.Plus(Duration.FromSeconds(authSettings?.RefreshTokenLifetime ?? DEFAULT_REFRESH_TOKEN_DURATION));
		Instant notBefore = now.Minus(Duration.FromSeconds(authSettings?.ClockSkewGraceSeconds ?? DEFAULT_CLOCK_SKEW_GRACE));

		OAuthBaseToken baseToken = new()
		{
			Issuer = "LEGACYSHIM.example.com",
			Subject = JwtSubject.FromInternal(user.Id).ToString(),
			Audiences = "example.com",
			ExpiresAt = expires.ToUnixTimeSeconds(),
			NotBeforeAt = notBefore.ToUnixTimeSeconds(),
			IssuedAt = now.ToUnixTimeSeconds(),
			JwtId = Guid.NewGuid(),
			Actor = null,
			ClientId = Guid.NewGuid(),
			Scopes = null,
		};

		return baseToken;
	}

	private async Task<User?> LegacyShimGetFullUserForTokens(
		int userId,
		CancellationToken cancellationToken = default)
	{
		// Get user from DB
		return await db.Users
			.AsNoTracking()
			.Include(x => x.Company)
			.Include(x => x.ProfileImageAsset)
			.Include(x => x.Culture)
			.Where(x => x.Id == userId)
			.FirstOrDefaultAsync(cancellationToken);
	}

	private class LegacyShimPrepareTokensResult
	{
		public User? User { get; set; }
		public OAuthBaseToken? BaseToken { get; set; }
		public RefreshToken? RefreshToken { get; set; }
		public AccessToken? AccessToken { get; set; }

		public bool Invalid
		{
			[MemberNotNullWhen(member: nameof(this.User), returnValue: false)]
			[MemberNotNullWhen(member: nameof(this.BaseToken), returnValue: false)]
			[MemberNotNullWhen(member: nameof(this.RefreshToken), returnValue: false)]
			[MemberNotNullWhen(member: nameof(this.AccessToken), returnValue: false)]
			get => this.User is null ||
				this.BaseToken is null ||
				this.RefreshToken is null ||
				this.AccessToken is null;
		}
	}

	private async Task<LegacyShimPrepareTokensResult> LegacyShimPrepareTokens(
		int userId,
		CancellationToken cancellationToken = default)
	{
		// ReSharper disable once UseObjectOrCollectionInitializer
		LegacyShimPrepareTokensResult result = new();

		result.User = await LegacyShimGetFullUserForTokens(
			userId: userId,
			cancellationToken: cancellationToken);

		if (result.User is null)
		{
			return result;
		}

		result.BaseToken = LegacyShimBuildBaseToken(result.User);

		result.RefreshToken = LegacyShimBuildRefreshToken(
			user: result.User,
			baseToken: result.BaseToken);

		result.AccessToken = await LegacyShimBuildAccessToken(
			user: result.User,
			baseToken: result.BaseToken,
			refreshToken: result.RefreshToken);

		return result;
	}

	public async Task<TokenResponse> RegisterUserSession(User user, CancellationToken cancellationToken = default)
	{
		TokenResponse responseData = new();

		LegacyShimPrepareTokensResult prepareTokens = await LegacyShimPrepareTokens(
			userId: user.Id,
			cancellationToken: cancellationToken);

		if (prepareTokens.Invalid)
		{
			throw new InvalidOperationException("Failed to create tokens");
		}

		responseData.AccessToken = jwtSerializer.Serialize(prepareTokens.AccessToken);
		responseData.RefreshToken = jwtSerializer.Serialize(prepareTokens.RefreshToken);

		// Register the session in the database
		UserSession session = new()
		{
			UserId = user.Id,
			AccessTokenGuid = prepareTokens.AccessToken.AccessTokenGuid,
			RefreshTokenGuid = prepareTokens.RefreshToken.RefreshTokenGuid,
			SessionCreatedOn = Instant.FromUnixTimeSeconds(prepareTokens.RefreshToken.IssuedAt).ToDateTimeUtc(),
			AccessTokenExpiresOn = prepareTokens.AccessToken.ExpiresOn,
			SessionExpiresOn = prepareTokens.RefreshToken.ExpiresOn,
		};

		user.LastLoginOn = session.SessionCreatedOn;

		_ = await db.Set<UserSession>().AddAsync(entity: session, cancellationToken: cancellationToken);
		_ = await db.SaveChangesAsync(cancellationToken);

		// Return responseData
		return responseData;
	}

	public async Task<BandaidDataModels.BandaidTokenResponse> BandaidRegisterUserSession(
		User user,
		CancellationToken cancellationToken = default
	)
	{
		BandaidDataModels.BandaidTokenResponse responseData = new();

		LegacyShimPrepareTokensResult prepareTokens = await LegacyShimPrepareTokens(
			userId: user.Id,
			cancellationToken: cancellationToken);

		if (prepareTokens.Invalid)
		{
			throw new InvalidOperationException("Failed to create tokens");
		}

		responseData.RefreshToken.Token = jwtSerializer.Serialize(prepareTokens.RefreshToken);
		responseData.RefreshToken.IssuedUtc = Instant.FromUnixTimeSeconds(prepareTokens.RefreshToken.IssuedAt).ToDateTimeUtc();
		responseData.RefreshToken.ExpiresUtc = prepareTokens.RefreshToken.ExpiresOn;

		responseData.AccessToken.Token = jwtSerializer.Serialize(prepareTokens.AccessToken);
		responseData.AccessToken.Issuer = prepareTokens.AccessToken.Issuer;
		responseData.AccessToken.UserId = prepareTokens.AccessToken.UserId;
		responseData.AccessToken.IssuedUtc = Instant.FromUnixTimeSeconds(prepareTokens.AccessToken.IssuedAt).ToDateTimeUtc();
		responseData.AccessToken.ExpiresUtc = prepareTokens.AccessToken.ExpiresOn;

		UserSession session = new()
		{
			UserId = user.Id,
			AccessTokenGuid = prepareTokens.AccessToken.AccessTokenGuid,
			RefreshTokenGuid = prepareTokens.RefreshToken.RefreshTokenGuid,
			SessionCreatedOn = Instant.FromUnixTimeSeconds(prepareTokens.RefreshToken.IssuedAt).ToDateTimeUtc(),
			AccessTokenExpiresOn = prepareTokens.AccessToken.ExpiresOn,
			SessionExpiresOn = prepareTokens.RefreshToken.ExpiresOn,
		};

		user.LastLoginOn = session.SessionCreatedOn;

		_ = await db.Set<UserSession>().AddAsync(entity: session, cancellationToken: cancellationToken);
		_ = await db.SaveChangesAsync(cancellationToken);

		return responseData;
	}

	public async Task<IResponse> ExchangeRefreshToken(string token, bool keepOldSession = false, CancellationToken cancellationToken = default)
	{
		// Parse the Refresh Token
		RefreshToken oldRefreshToken;

		try
		{
			oldRefreshToken = jwtSerializer.Deserialize<RefreshToken>(token);
		}
		catch (Exception)
		{
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Lookup the user session
		UserSession? session = await db.Set<UserSession>()
			.Where(x => x.RefreshTokenGuid == oldRefreshToken.RefreshTokenGuid)
			.FirstOrDefaultAsync(cancellationToken);
		if (session is null)
		{
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Ensure session is not expired
		if (session.SessionExpiresOn <= DateTime.UtcNow)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Lookup the user
		User? user = await db.Set<User>()
			.Where(x => x.Id == session.UserId)
			.FirstOrDefaultAsync(cancellationToken);
		if (user is null)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Ensure user is active
		if (user.Status != Status.ACTIVE)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			return Response.FromError(Enums.ErrorCodes.Authorization.UNAUTHORIZED);
		}

		// Lookup the company
		Company? company = await db.Set<Company>()
			.Where(x => x.Id == user.CompanyId)
			.FirstOrDefaultAsync(cancellationToken);
		if (company is null)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Ensure company is active
		if (company.Status != Status.ACTIVE)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			return Response.FromError(Enums.ErrorCodes.Authorization.UNAUTHORIZED);
		}

		// Prepare responseData
		TokenResponse responseData = new();

		LegacyShimPrepareTokensResult prepareTokens = await LegacyShimPrepareTokens(
			userId: user.Id,
			cancellationToken: cancellationToken);

		if (prepareTokens.Invalid)
		{
			throw new InvalidOperationException("Failed to create tokens");
		}

		DateTime refreshIssuedAtDateTime = Instant.FromUnixTimeSeconds(prepareTokens.RefreshToken.IssuedAt).ToDateTimeUtc();

		// Bring other authentication fields from oidc from old refresh token
		prepareTokens.RefreshToken.AuthenticationTime
			= oldRefreshToken.AuthenticationTime;

		prepareTokens.RefreshToken.AuthenticationContextClassReference
			= oldRefreshToken.AuthenticationContextClassReference;

		prepareTokens.RefreshToken.AuthenticationMethodsReference
			= oldRefreshToken.AuthenticationMethodsReference;

		prepareTokens.AccessToken.AuthenticationTime
			= oldRefreshToken.AuthenticationTime;

		prepareTokens.AccessToken.AuthenticationContextClassReference
			= oldRefreshToken.AuthenticationContextClassReference;

		prepareTokens.AccessToken.AuthenticationMethodsReference
			= oldRefreshToken.AuthenticationMethodsReference;

		responseData.RefreshToken = jwtSerializer.Serialize(prepareTokens.RefreshToken);
		responseData.AccessToken = jwtSerializer.Serialize(prepareTokens.AccessToken);

		// Update the session
		if (keepOldSession)
		{
			db.Add(new UserSession()
			{
				UserId = user.Id,
				AccessTokenGuid = prepareTokens.AccessToken.AccessTokenGuid,
				RefreshTokenGuid = prepareTokens.RefreshToken.RefreshTokenGuid,
				AccessTokenExpiresOn = prepareTokens.AccessToken.ExpiresOn,
				SessionCreatedOn = refreshIssuedAtDateTime,
				SessionExpiresOn = prepareTokens.RefreshToken.ExpiresOn,
			});
		}
		else
		{
			session.AccessTokenGuid = prepareTokens.AccessToken.AccessTokenGuid;
			session.RefreshTokenGuid = prepareTokens.RefreshToken.RefreshTokenGuid;
			session.AccessTokenExpiresOn = prepareTokens.AccessToken.ExpiresOn;
			session.SessionExpiresOn = prepareTokens.RefreshToken.ExpiresOn;
		}

		user.LastLoginOn = refreshIssuedAtDateTime;
		_ = await db.SaveChangesAsync(cancellationToken);

		// Return the new tokens
		return Response.FromSuccess().WithData(responseData);
	}

	public async Task<IResponse> BandaidExchangeRefreshToken(
		string token,
		bool keepOldSession = false,
		CancellationToken cancellationToken = default
	)
	{
		// Parse the Refresh Token
		RefreshToken oldRefreshToken;

		try
		{
			oldRefreshToken = jwtSerializer.Deserialize<RefreshToken>(token);
		}
		catch (Exception)
		{
			logger.LogDebug("Refresh Token failed parsing");
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Lookup the user session
		UserSession? session = await db.Set<UserSession>()
			.Where(x => x.RefreshTokenGuid == oldRefreshToken.RefreshTokenGuid)
			.FirstOrDefaultAsync(cancellationToken);
		if (session is null)
		{
			logger.LogDebug("User Session was not found");
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Ensure session is not expired
		if (session.SessionExpiresOn <= DateTime.UtcNow)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			logger.LogDebug("User Session was expired");
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Lookup the user
		User? user = await db.Set<User>()
			.Where(x => x.Id == session.UserId)
			.FirstOrDefaultAsync(cancellationToken);
		if (user is null)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			logger.LogDebug("User was not found");
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Ensure user is active
		if (user.Status != Status.ACTIVE)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			logger.LogDebug("User was not active");
			return Response.FromError(Enums.ErrorCodes.Authorization.UNAUTHORIZED);
		}

		// Lookup the company
		Company? company = await db.Set<Company>()
			.Where(x => x.Id == user.CompanyId)
			.FirstOrDefaultAsync(cancellationToken);
		if (company is null)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			logger.LogDebug("Company was not found");
			return Response.FromError(Enums.ErrorCodes.Authentication.BAD_REFRESH_TOKEN);
		}

		// Ensure company is active
		if (company.Status != Status.ACTIVE)
		{
			_ = db.Remove(session);
			_ = await db.SaveChangesAsync(cancellationToken);
			logger.LogDebug("Company was not active");
			return Response.FromError(Enums.ErrorCodes.Authorization.UNAUTHORIZED);
		}

		// Prepare responseData
		BandaidDataModels.BandaidTokenResponse responseData = new();

		LegacyShimPrepareTokensResult prepareTokens = await LegacyShimPrepareTokens(
			userId: user.Id,
			cancellationToken: cancellationToken);

		if (prepareTokens.Invalid)
		{
			throw new InvalidOperationException("Failed to create tokens");
		}

		DateTime refreshIssuedAtDateTime = Instant.FromUnixTimeSeconds(prepareTokens.RefreshToken.IssuedAt).ToDateTimeUtc();
		DateTime accessIssuedAtDateTime = Instant.FromUnixTimeSeconds(prepareTokens.AccessToken.IssuedAt).ToDateTimeUtc();

		// Bring other authentication fields from oidc from old refresh token
		prepareTokens.RefreshToken.AuthenticationTime
			= oldRefreshToken.AuthenticationTime;

		prepareTokens.RefreshToken.AuthenticationContextClassReference
			= oldRefreshToken.AuthenticationContextClassReference;

		prepareTokens.RefreshToken.AuthenticationMethodsReference
			= oldRefreshToken.AuthenticationMethodsReference;

		prepareTokens.AccessToken.AuthenticationTime
			= oldRefreshToken.AuthenticationTime;

		prepareTokens.AccessToken.AuthenticationContextClassReference
			= oldRefreshToken.AuthenticationContextClassReference;

		prepareTokens.AccessToken.AuthenticationMethodsReference
			= oldRefreshToken.AuthenticationMethodsReference;

		responseData.RefreshToken.Token = jwtSerializer.Serialize(prepareTokens.RefreshToken);
		responseData.RefreshToken.IssuedUtc = refreshIssuedAtDateTime;
		responseData.RefreshToken.ExpiresUtc = prepareTokens.RefreshToken.ExpiresOn;

		responseData.AccessToken.Token = jwtSerializer.Serialize(prepareTokens.AccessToken);
		responseData.AccessToken.Issuer = prepareTokens.AccessToken.Issuer;
		responseData.AccessToken.UserId = prepareTokens.AccessToken.UserId;
		responseData.AccessToken.IssuedUtc = accessIssuedAtDateTime;
		responseData.AccessToken.ExpiresUtc = prepareTokens.AccessToken.ExpiresOn;

		// Update the session
		if (keepOldSession)
		{
			db.Add(new UserSession()
			{
				UserId = user.Id,
				AccessTokenGuid = prepareTokens.AccessToken.AccessTokenGuid,
				RefreshTokenGuid = prepareTokens.RefreshToken.RefreshTokenGuid,
				AccessTokenExpiresOn = prepareTokens.AccessToken.ExpiresOn,
				SessionCreatedOn = refreshIssuedAtDateTime,
				SessionExpiresOn = prepareTokens.RefreshToken.ExpiresOn,
			});
		}
		else
		{
			session.AccessTokenGuid = prepareTokens.AccessToken.AccessTokenGuid;
			session.RefreshTokenGuid = prepareTokens.RefreshToken.RefreshTokenGuid;
			session.AccessTokenExpiresOn = prepareTokens.AccessToken.ExpiresOn;
			session.SessionExpiresOn = prepareTokens.RefreshToken.ExpiresOn;
		}

		user.LastLoginOn = refreshIssuedAtDateTime;
		_ = await db.SaveChangesAsync(cancellationToken);

		// Return the new tokens
		return Response.FromSuccess().WithData(responseData);
	}

	public AccessToken? ParseCurrentAccessToken()
	{
		if (httpContextAccessor.HttpContext is null)
		{
			throw new InvalidOperationException(
				"AuthorizationService could not access the HttpContext via the HttpContextAccessor!");
		}

		string? rawAccessToken =
			EndpointAuthService.GetRawAccessTokenFromHeader(httpContextAccessor.HttpContext)
			?? EndpointAuthService.GetRawAccessTokenFromCookie(httpContextAccessor.HttpContext);

		if (rawAccessToken is null) return null;

		return jwtSerializer.Deserialize<AccessToken>(rawAccessToken);
	}

	public bool OldCheckPermissions(
		Permissions permissions,
		[NotNullWhen(true)] out AccessToken? accessToken,
		out IResponse permissionCheckResponse
	)
	{
		accessToken = ParseCurrentAccessToken();
		permissionCheckResponse = Response.FromSuccess();

		if (accessToken is null)
		{
			permissionCheckResponse = Response.FromError(Enums.ErrorCodes.Authentication.INVALID_ACCESS_TOKEN);
			return false;
		}

		if (permissions.HasFlag(Permissions.USER))
		{
			return true;
		}

		// ReSharper disable once ReplaceWithSingleAssignment.False
		bool hasPermission = false;

		// ReSharper disable once ConvertIfToOrExpression
		if (permissions.HasFlag(Permissions.OPERATOR) && accessToken.IsOperator)
		{
			hasPermission = true;
		}

		if (permissions.HasFlag(Permissions.DEVELOPER) && accessToken.IsDeveloper)
		{
			hasPermission = true;
		}

		if (permissions.HasFlag(Permissions.SUPER_ADMIN) && accessToken.IsSuperAdmin)
		{
			hasPermission = true;
		}

		if (permissions.HasFlag(Permissions.BILLING_ADMING) && accessToken.IsBillingAdmin)
		{
			hasPermission = true;
		}

		if (permissions.HasFlag(Permissions.USER_ADMIN) && accessToken.IsUserAdmin)
		{
			hasPermission = true;
		}

		if (hasPermission)
		{
			return true;
		}

		permissionCheckResponse = Response.FromError(Enums.ErrorCodes.Authorization.UNAUTHORIZED);
		return false;
	}
}
