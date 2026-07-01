using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Platform.Common.Json;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Platform.Common.Auth;

/// <summary>
/// The old token format before auth changes
/// This Type derives from <see cref="PlatformAccessToken"/>
/// </summary>
[Obsolete("Old Token Format")]
[SuppressMessage(category: "ReSharper", checkId: "UnusedType.Global")]
public class LegacyPlatformAccessToken : PlatformAccessToken
{
	[JsonPropertyName("accessTokenGuid")]
	public Guid AccessTokenGuid { get; set; }

	[JsonPropertyName("expiresOn")]
	public DateTime ExpiresOn { get; set; }

	[JsonPropertyName("userId")]
	public int UserId { get; set; }

	[JsonPropertyName("companyId")]
	public int CompanyId { get; set; }

	[JsonPropertyName("userName")]
	public string? UserName { get; set; }

	[JsonPropertyName("userEmailAddress")]
	public string? UserEmailAddress { get; set; }

	[JsonPropertyName("friendlyName")]
	public string? FriendlyName { get; set; }

	[JsonPropertyName("initials")]
	public string? Initials { get; set; }

	[JsonPropertyName("profileImageAssetId")]
	public int? ProfileImageAssetId { get; set; }

	[JsonPropertyName("profileImageAssetKey")]
	public Guid ProfileImageAssetKey { get; set; }

	[JsonPropertyName("companyName")]
	public string? CompanyName { get; set; }

	[JsonPropertyName("cultureLanguageCode")]
	public string? CultureLanguageCode { get; set; }

	[JsonPropertyName("cultureCountryCode")]
	public string? CultureCountryCode { get; set; }

	[JsonPropertyName("isDeveloper")]
	public bool IsDeveloper { get; set; }

	[JsonPropertyName("isOperator")]
	public bool IsOperator { get; set; }

	[JsonPropertyName("isSuperAdmin")]
	public bool IsSuperAdmin { get; set; }

	[JsonPropertyName("isBillingAdmin")]
	public bool IsBillingAdmin { get; set; }

	[JsonPropertyName("isUserAdmin")]
	public bool IsUserAdmin { get; set; }

	[JsonPropertyName("isAnyCompanyAdmin")]
	public bool IsAnyCompanyAdmin => IsSuperAdmin || IsBillingAdmin || IsUserAdmin;

	[JsonPropertyName("isAnyOperator")]
	public bool IsAnyOperator => IsOperator || IsDeveloper;

	[JsonPropertyName("isAnyAdmin")]
	public bool IsAnyAdmin => IsAnyCompanyAdmin || IsAnyOperator;
}