using Platform.Common.Auth;

namespace Platform.Legacy.Core.Models.Auth;

#pragma warning disable CS0618 // Type or member is obsolete
public class AccessToken : LegacyPlatformAccessToken
#pragma warning restore CS0618 // Type or member is obsolete
{
	/*public void ConfigureMapping(IMapperConfigurationExpression config)
	{
		_ = config
			.CreateMap<User, AccessToken>()
			.ForMember(destinationMember: d => d.AccessTokenGuid, memberOptions: o => o.Ignore())
			.ForMember(destinationMember: d => d.ExpiresOn, memberOptions: o => o.Ignore())
			.ForMember(destinationMember: d => d.UserId, memberOptions: o => o.MapFrom(s => s.Id))
			.Prop(destination: d => d.FriendlyName, source: s => s.FriendlyName)
			.Prop(destination: d => d.Initials, source: s => s.Initials)
			.Prop(destination: d => d.UserEmailAddress, source: s => s.Email)
			.ForMember(destinationMember: d => d.CompanyName, memberOptions: o => o.MapFrom(s => s.Company.Name))
			.ForMember(
				destinationMember: d => d.CultureLanguageCode,
				memberOptions: o => o.MapFrom(s => s.Culture != null ? s.Culture.OfficialLanguageCode : null)
			)
			.ForMember(
				destinationMember: d => d.CultureCountryCode,
				memberOptions: o => o.MapFrom(s => s.Culture != null ? s.Culture.CountryCode : null)
			)
			//.ForMember(d => d.IsDeveloper, o => o.MapFrom(s => s.UserRoles.Any(r => r.Role.Name == Roles.DEVELOPER)))
			//.ForMember(d => d.IsOperator, o => o.MapFrom(s => s.UserRoles.Any(r => r.Role.Name == Roles.OPERATOR)))
			.ForMember(destinationMember: d => d.IsDeveloper, memberOptions: o => o.Ignore())
			.ForMember(destinationMember: d => d.IsOperator, memberOptions: o => o.Ignore())
			.ForMember(destinationMember: d => d.IsSuperAdmin, memberOptions: o => o.MapFrom(s => s.SuperAdmin))
			.ForMember(destinationMember: d => d.IsBillingAdmin, memberOptions: o => o.MapFrom(s => s.BillingAdmin))
			.ForMember(destinationMember: d => d.IsUserAdmin, memberOptions: o => o.MapFrom(s => s.UserAdmin))
			.ForMember(destinationMember: d => d.IsAnyCompanyAdmin, memberOptions: o => o.Ignore())
			.ForMember(destinationMember: d => d.IsAnyOperator, memberOptions: o => o.Ignore())
			.ForMember(destinationMember: d => d.IsAnyAdmin, memberOptions: o => o.Ignore())
			.Prop(
				destination: d => d.ProfileImageAssetKey,
				source: s => s.ProfileImageAsset != null ? s.ProfileImageAsset.PublicId : Guid.Empty
			);
	}

	public static async Task<AccessToken?> NewAsync(
		User user,
		LegacyDbContext db,
		UserManager<User> userManager,
		IMapper mapper,
		SettingsService settingsService,
		CancellationToken cancellationToken = default
	)
	{
		AccessToken? token = await db
			.Users.AsNoTrackingWithIdentityResolution()
			.Where(x => x.Id == user.Id)
			.ProjectTo<AccessToken>(mapper.ConfigurationProvider)
			.FirstOrDefaultAsync(cancellationToken);

		if (token is null)
		{
			return null;
		}

		token.AccessTokenGuid = Guid.NewGuid();
		token.ExpiresOn = DateTime.UtcNow.AddSeconds(settingsService.Auth?.AccessTokenLifetime ?? 300);
		token.IsDeveloper = await userManager.IsInRoleAsync(user: user, role: Roles.DEVELOPER);
		token.IsOperator = await userManager.IsInRoleAsync(user: user, role: Roles.OPERATOR);

		return token;
	}

	public static AccessToken? FromClaimsPrincipal(ClaimsPrincipal? principal)
	{
		if (principal is null)
		{
			return null;
		}

		AccessToken token = new();

		string? stringAccessTokenGuid = principal.FindFirstValue(nameof(AccessToken.AccessTokenGuid).CamelCase());
		if (!Guid.TryParse(input: stringAccessTokenGuid, result: out Guid accessTokenGuid))
		{
			return null;
		}
		token.AccessTokenGuid = accessTokenGuid;

		string? stringExpiresOn = principal.FindFirstValue(nameof(AccessToken.ExpiresOn).CamelCase());
		if (!long.TryParse(s: stringExpiresOn, result: out long longExpiresOn))
		{
			return null;
		}
		token.ExpiresOn = DateTime.FromBinary(longExpiresOn);

		string? stringUserId = principal.FindFirstValue(nameof(AccessToken.UserId).CamelCase());
		if (!int.TryParse(s: stringUserId, result: out int userId))
		{
			return null;
		}
		token.UserId = userId;

		string? stringCompanyId = principal.FindFirstValue(nameof(AccessToken.CompanyId).CamelCase());
		if (!int.TryParse(s: stringCompanyId, result: out int companyId))
		{
			return null;
		}
		token.CompanyId = companyId;

		string? stringProfileImageAssetId = principal.FindFirstValue(nameof(AccessToken.ProfileImageAssetId).CamelCase());
		if (int.TryParse(s: stringProfileImageAssetId, result: out int profileImageAssetId))
		{
			token.ProfileImageAssetId = profileImageAssetId;
		}

		string? stringProfileImageAssetKey = principal.FindFirstValue(nameof(AccessToken.ProfileImageAssetKey).CamelCase());
		if (!Guid.TryParse(input: stringProfileImageAssetKey, result: out Guid profileImageKey))
		{
			token.ProfileImageAssetKey = Guid.NewGuid();
		}
		token.ProfileImageAssetKey = profileImageKey;

		token.UserName = principal.FindFirstValue(nameof(AccessToken.UserName).CamelCase());
		token.UserEmailAddress = principal.FindFirstValue(nameof(AccessToken.UserEmailAddress).CamelCase());
		token.FriendlyName = principal.FindFirstValue(nameof(AccessToken.FriendlyName).CamelCase());
		token.Initials = principal.FindFirstValue(nameof(AccessToken.Initials).CamelCase());
		token.CompanyName = principal.FindFirstValue(nameof(AccessToken.CompanyName).CamelCase());
		token.CultureLanguageCode = principal.FindFirstValue(nameof(AccessToken.CultureLanguageCode).CamelCase());
		token.CultureCountryCode = principal.FindFirstValue(nameof(AccessToken.CultureCountryCode).CamelCase());
		token.IsDeveloper =
			bool.TryParse(value: principal.FindFirstValue(nameof(AccessToken.IsDeveloper).CamelCase()), result: out bool isDeveloper)
			&& isDeveloper;
		token.IsOperator =
			bool.TryParse(value: principal.FindFirstValue(nameof(AccessToken.IsOperator).CamelCase()), result: out bool isOperator) && isOperator;
		token.IsSuperAdmin =
			bool.TryParse(value: principal.FindFirstValue(nameof(AccessToken.IsSuperAdmin).CamelCase()), result: out bool isSuperAdmin)
			&& isSuperAdmin;
		token.IsBillingAdmin =
			bool.TryParse(value: principal.FindFirstValue(nameof(AccessToken.IsBillingAdmin).CamelCase()), result: out bool isBillingAdmin)
			&& isBillingAdmin;
		token.IsUserAdmin =
			bool.TryParse(value: principal.FindFirstValue(nameof(AccessToken.IsUserAdmin).CamelCase()), result: out bool isUserAdmin)
			&& isUserAdmin;

		return token;
	}

	public List<Claim> ToClaims()
	{
		return
		[
			new Claim(type: nameof(AccessToken.AccessTokenGuid).CamelCase(), value: this.AccessTokenGuid.ToString("N").ToLowerInvariant()),
			new Claim(type: nameof(AccessToken.ExpiresOn).CamelCase(), value: this.ExpiresOn.ToBinary().ToString()),
			new Claim(type: nameof(AccessToken.UserId).CamelCase(), value: this.UserId.ToString()),
			new Claim(type: nameof(AccessToken.CompanyId).CamelCase(), value: this.CompanyId.ToString()),
			new Claim(type: nameof(AccessToken.UserName).CamelCase(), value: this.UserName ?? ""),
			new Claim(type: nameof(AccessToken.UserEmailAddress).CamelCase(), value: this.UserEmailAddress ?? ""),
			new Claim(type: nameof(AccessToken.FriendlyName).CamelCase(), value: this.FriendlyName ?? ""),
			new Claim(type: nameof(AccessToken.Initials).CamelCase(), value: this.Initials ?? ""),
			new Claim(type: nameof(AccessToken.ProfileImageAssetId).CamelCase(), value: this.ProfileImageAssetId.ToString() ?? ""),
			new Claim(type: nameof(AccessToken.ProfileImageAssetKey).CamelCase(), value: this.ProfileImageAssetKey.ToString()),
			new Claim(type: nameof(AccessToken.CompanyName).CamelCase(), value: this.CompanyName ?? ""),
			new Claim(type: nameof(AccessToken.CultureLanguageCode).CamelCase(), value: this.CultureLanguageCode ?? ""),
			new Claim(type: nameof(AccessToken.CultureCountryCode).CamelCase(), value: this.CultureCountryCode ?? ""),
			new Claim(type: nameof(AccessToken.IsOperator).CamelCase(), value: this.IsOperator.ToString()),
			new Claim(type: nameof(AccessToken.IsDeveloper).CamelCase(), value: this.IsDeveloper.ToString()),
			new Claim(type: nameof(AccessToken.IsSuperAdmin).CamelCase(), value: this.IsSuperAdmin.ToString()),
			new Claim(type: nameof(AccessToken.IsBillingAdmin).CamelCase(), value: this.IsBillingAdmin.ToString()),
			new Claim(type: nameof(AccessToken.IsUserAdmin).CamelCase(), value: this.IsUserAdmin.ToString()),
		];
	}*/

	/*public static AccessToken? FromJwt(string token, JwtHelperService jwtHelperService)
	{
		return AccessToken.FromClaimsPrincipal(jwtHelperService.ParseToken(token));
	}

	public string ToJwt(JwtHelperService jwtHelperService, ApiConfiguration apiConfiguration)
	{
		DateTime notBefore = DateTime.UtcNow.AddMinutes(apiConfiguration.TokensAccessTokenNotBeforeGraceMinutes);
		return jwtHelperService.BuildToken(claims: ToClaims(), notBefore: notBefore, expires: this.ExpiresOn);
	}*/
}
