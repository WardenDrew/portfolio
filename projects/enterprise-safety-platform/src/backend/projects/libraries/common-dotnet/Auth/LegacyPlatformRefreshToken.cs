using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Platform.Common.Auth;

/// <summary>
/// The old token format before auth changes
/// This Type derives from <see cref="PlatformRefreshToken"/>
/// </summary>
[Obsolete("Old token format")]
[SuppressMessage(category: "ReSharper", checkId: "UnusedType.Global")]
public class LegacyPlatformRefreshToken : PlatformRefreshToken
{
	[JsonPropertyName("refreshTokenGuid")]
	public required Guid RefreshTokenGuid { get; set; }
	
	[JsonPropertyName("expiresOn")]
	public required DateTime ExpiresOn { get; set; }
}