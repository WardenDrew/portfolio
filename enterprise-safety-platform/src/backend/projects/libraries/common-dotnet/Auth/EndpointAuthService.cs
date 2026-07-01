
#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Platform.Common.Jwt;
using Platform.Common.Permissions;

namespace Platform.Common.Auth;

/// <summary>
/// Endpoint Auth Service for checking authentication and authorization
/// </summary>
public class EndpointAuthService
{
	/// <summary>
	/// The cookie name for the access token
	/// </summary>
	public const string ACCESS_TOKEN_COOKIE_KEY = "access_token";
	
	private readonly string? rawAccessToken;
	private readonly PlatformAccessToken? accessToken;
	private readonly LegacyPlatformAccessToken? legacyAccessToken;
	private readonly string? accessTokenSource;
	private readonly List<string> accessTokenScopes = [];

	/// <summary>
	/// Endpoint Auth Service Constructor
	/// </summary>
	/// <param name="httpContextAccessor"></param>
	/// <param name="jwtSerializer"></param>
	/// <param name="logger"></param>
	public EndpointAuthService(
		IHttpContextAccessor httpContextAccessor,
		JwtSerializer jwtSerializer,
		ILogger<EndpointAuthService> logger)
	{
		if (httpContextAccessor.HttpContext is null)
		{
			throw new InvalidOperationException(
				"EndpointAuthService could not access the HttpContext via the HttpContextAccessor!");
		}

		// Check header first
		rawAccessToken = EndpointAuthService.GetRawAccessTokenFromHeader(httpContextAccessor.HttpContext);
		if (rawAccessToken is not null)
		{
			accessTokenSource = "header";
		}
		
		// Check for Cookie next if header didn't have the access token
		if (rawAccessToken is null)
		{
			rawAccessToken = EndpointAuthService.GetRawAccessTokenFromCookie(httpContextAccessor.HttpContext);
			if (rawAccessToken is not null)
			{
				accessTokenSource = "cookie";
			}
		}

		// If we got an access token parse it
		if (rawAccessToken is not null)
		{
			try
			{
				legacyAccessToken = jwtSerializer.Deserialize<LegacyPlatformAccessToken>(rawAccessToken);
			}
			catch (Exception)
			{
				logger.LogWarning("rawAccessToken failed to deserialize as LegacyPlatformAccessToken");
			}

			try
			{
				accessToken = jwtSerializer.Deserialize<PlatformAccessToken>(rawAccessToken);
			}
			catch (Exception)
			{
				logger.LogWarning("rawAccessToken failed to deserialize as PlatformAccessToken");
			}
		}
		
		// Parse the scopes
		if (accessToken?.Scopes is not null)
		{
			accessTokenScopes = accessToken.Scopes
				.Split(separator: ' ', options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToList();
		}
		else if (legacyAccessToken is not null)
		{
			// Legacy scopes calculation
			
			accessTokenScopes.Add(Scope.USER);
			
			if (legacyAccessToken.IsOperator || legacyAccessToken.IsDeveloper )
			{
				accessTokenScopes.Add(Scope.SYSTEM);
			}

			if (legacyAccessToken.IsSuperAdmin)
			{
				accessTokenScopes.Add(Scope.ORG);
			}
			else if (legacyAccessToken.IsBillingAdmin)
			{
				accessTokenScopes.Add(Scope.ORG_BILLING);
			}
			else if (legacyAccessToken.IsUserAdmin)
			{
				accessTokenScopes.Add(Scope.ORG_USERS);
				accessTokenScopes.Add(Scope.ORG_TIMECLOCKS);
			}
		}
	}

	/// <summary>
	/// These should move to a helper
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public static string? GetRawAccessTokenFromHeader(HttpContext context)
	{
		string? authHeader = context.Request.Headers.Authorization.FirstOrDefault();

		if (authHeader is null) return null;
		
		return EndpointAuthService.parseAuthHeader(authHeader);
	}

	/// <summary>
	/// These should move to a helper
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public static string? GetRawAccessTokenFromCookie(HttpContext context)
	{
		context.Request.Cookies.TryGetValue(
			key: EndpointAuthService.ACCESS_TOKEN_COOKIE_KEY, 
			value: out string? localrawAccessToken);

		return localrawAccessToken;
	}

	/// <summary>
	/// Parse the authorization request header
	/// </summary>
	/// <param name="authHeader"></param>
	/// <returns></returns>
	private static string? parseAuthHeader(string authHeader)
	{
		string[] authHeaderParts = authHeader.Split(
			separator: ' ',
			count: 2,
			options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (authHeaderParts.Length != 2)
		{
			return null;
		}

		if (!authHeaderParts[0].Equals(value: "Bearer", comparisonType: StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		return authHeaderParts[1];
	}

	/// <summary>
	/// The AccessToken provided for the request
	/// </summary>
	// ReSharper disable once ConvertToAutoPropertyWhenPossible
	public PlatformAccessToken? AccessToken => accessToken;
	
	/// <summary>
	/// The LegacyAccessToken provided for the request
	/// </summary>
	// ReSharper disable once ConvertToAutoPropertyWhenPossible
	public LegacyPlatformAccessToken? LegacyAccessToken => legacyAccessToken;

	/// <summary>
	/// The source of the access token for the request
	/// </summary>
	// ReSharper disable once ConvertToAutoProperty
	public string? AccessTokenSource => accessTokenSource;

	/// <summary>
	/// The Raw access token itself
	/// </summary>
	public string? RawAccessToken => rawAccessToken;

	/// <summary>
	/// The Scopes parsed from the access token
	/// </summary>
	// ReSharper disable once ConvertToAutoPropertyWhenPossible
	public List<string> Scopes => accessTokenScopes;
	
	/// <summary>
	/// If this is an authenticated user
	/// </summary>
	[MemberNotNullWhen(returnValue: true, member: nameof(EndpointAuthService.accessToken))]
	[MemberNotNullWhen(returnValue: true, member: nameof(EndpointAuthService.AccessToken))]
	public bool IsAuthenticated => accessToken is not null;

	// Cached result after first calc
	private int? platformUserId;
	/// <summary>
	/// Get the PlatformUserId from the subject claim
	/// </summary>
	public int? PlatformUserId
	{
		get
		{
			if (accessToken is null) return null;
			if (platformUserId is not null) return platformUserId;

			platformUserId = JwtSubject
				.Parse(accessToken.Subject)
				?.ToInternal();
			
			return platformUserId;
		}
	}

	/// <summary>
	/// Organization Id
	/// </summary>
	public int? OrgId => accessToken?.OrgId;
	
	/// <summary>
	/// Organization-User Id
	/// </summary>
	public int? OrgUserId => accessToken?.OrgUserId;

	/// <summary>
	/// If the access token contains ANY of the following scopes
	/// </summary>
	/// <param name="scopes"></param>
	/// <returns></returns>
	public bool HasAnyScope(params string[] scopes)
	{
		if (accessToken is null) return false;
		return accessTokenScopes.Intersect(scopes).Any();
	}

	/// <summary>
	/// If the access token contains ALL the following scopes
	/// </summary>
	/// <param name="scopes"></param>
	/// <returns></returns>
	public bool HasAllScopes(params string[] scopes)
	{
		if (accessToken is null) return false;
		
		// Reduces parameter scopes to only values that are not in accessToken.Scopes then inverts the result of
		// anything in the list. If all the scopes in parameter scopes are in the accessToken there will be nothing
		// left and the result is true
		return !scopes.Except(accessTokenScopes).Any(); 
	}
	
	/// <summary>
	/// Throws a <see cref="PlatformUnauthenticatedException"/> if the request is not authenticated
	/// </summary>
	/// <exception cref="PlatformUnauthenticatedException"></exception>
	[MemberNotNull(nameof(EndpointAuthService.accessToken))]
	[MemberNotNull(nameof(EndpointAuthService.AccessToken))]
	public void ThrowIfNotAuthenticated()
	{
		if (!IsAuthenticated)
		{
			throw new PlatformUnauthenticatedException();
		}
	}

	/// <summary>
	/// Throws a <see cref="PlatformUnauthorizedException"/> if the request token does not have org information.
	/// Calls <see cref="ThrowIfNotAuthenticated"/>
	/// </summary>
	/// <exception cref="PlatformUnauthorizedException"></exception>
	[MemberNotNull(nameof(EndpointAuthService.accessToken))]
	[MemberNotNull(nameof(EndpointAuthService.AccessToken))]
	[MemberNotNull(nameof(EndpointAuthService.OrgId))]
	[MemberNotNull(nameof(EndpointAuthService.OrgUserId))]
	public void ThrowIfNotOrgBound()
	{
		ThrowIfNotAuthenticated();
		if (OrgId is null || OrgUserId is null)
		{
			throw new PlatformUnauthorizedException("This endpoints requires an access token bound to an organization");
		}
	}
	
	/// <summary>
	/// Throws a <see cref="PlatformUnauthenticatedException"/> if the request is not from a Platform Internal User
	/// Calls <see cref="ThrowIfNotAuthenticated"/>
	/// </summary>
	/// <exception cref="PlatformUnauthorizedException"></exception>
	[MemberNotNull(nameof(EndpointAuthService.accessToken))]
	[MemberNotNull(nameof(EndpointAuthService.AccessToken))]
	[MemberNotNull(nameof(EndpointAuthService.PlatformUserId))]
	[MemberNotNull(nameof(EndpointAuthService.platformUserId))]
	public void ThrowIfNotPlatformInternalUser()
	{
		ThrowIfNotAuthenticated();
		if (PlatformUserId is null || platformUserId is null)
		{
			throw new PlatformUnauthorizedException("This endpoints requires a Platform Internal User to access");
		}
	}

	/// <summary>
	/// Throws a <see cref="PlatformUnauthorizedException"/> if the request token does not have one of the scopes
	/// Calls <see cref="ThrowIfNotAuthenticated"/>
	/// </summary>
	/// <param name="scopes"></param>
	/// <exception cref="PlatformUnauthorizedException"></exception>
	public void ThrowIfNotAnyScope(params string[] scopes)
	{
		ThrowIfNotAuthenticated();
		if (!HasAnyScope(scopes))
		{
			throw new PlatformUnauthorizedException("Access token has none of the possible scopes for this endpoint");
		}
	}
	
	/// <summary>
	/// Throws a <see cref="PlatformUnauthorizedException"/> if the request token does not have all the scopes
	/// Calls <see cref="ThrowIfNotAuthenticated"/>
	/// </summary>
	/// <param name="scopes"></param>
	/// <exception cref="PlatformUnauthorizedException"></exception>
	public void ThrowIfNotAllScopes(params string[] scopes)
	{
		ThrowIfNotAuthenticated();
		if (!HasAllScopes(scopes))
		{
			throw new PlatformUnauthorizedException("Access token is missing one of the required scopes for this endpoint");
		}
	}
}

/// <summary>
/// An exception thrown to break endpoint execution when the request was not authenticated
/// Generally we shouldn't see this occur unless an endpoint is erroneously marked as AllowAnonymous()
/// </summary>
public class PlatformUnauthenticatedException : Exception { }

/// <summary>
/// An exception thrown to break endpoint execution when the request was not authorized
/// The Exception message contains information about why the request was not authorized
/// </summary>
public class PlatformUnauthorizedException : Exception
{
	/// <summary>
	/// An unauthorized exception without further details
	/// </summary>
	public PlatformUnauthorizedException() { }

	/// <summary>
	/// An unauthorized exception that explains the reason for the unauthorized access
	/// </summary>
	/// <param name="message"></param>
	public PlatformUnauthorizedException(string message) : base(message) { }
}