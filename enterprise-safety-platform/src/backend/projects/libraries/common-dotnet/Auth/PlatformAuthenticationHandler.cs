using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
// ReSharper disable SpecifyACultureInStringConversionExplicitly
#pragma warning disable CS0618 // Type or member is obsolete

namespace Platform.Common.Auth;

/// <inheritdoc />
public class PlatformAuthenticationHandler(
	IOptionsMonitor<PlatformAuthenticationSchemeOptions> options, 
	ILoggerFactory logger, 
	UrlEncoder encoder,
	EndpointAuthService endpointAuthService) 
	: AuthenticationHandler<PlatformAuthenticationSchemeOptions>(
		options: options, 
		logger: logger, 
		encoder: encoder)
{
	/// <inheritdoc />
	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		await Task.CompletedTask;
		
		if (!endpointAuthService.IsAuthenticated)
		{
			if (endpointAuthService.RawAccessToken is not null) {
				return AuthenticateResult.Fail("Malformed access token");
			}
			
			return AuthenticateResult.NoResult();
		}

		List<Claim> claims =
		[
			new(type: "iss", value: endpointAuthService.AccessToken.Issuer),
			new(type: "sub", value: endpointAuthService.AccessToken.Subject),
			new(type: "exp", value: endpointAuthService.AccessToken.ExpiresAt.ToString()),
			new(type: "nbf", value: endpointAuthService.AccessToken.NotBeforeAt.ToString()),
		];

		ClaimsIdentity claimsIdentity = new(claims: claims, authenticationType: "AccessToken");
		ClaimsPrincipal claimsPrincipal = new(claimsIdentity);
		AuthenticationTicket authenticationTicket = new(principal: claimsPrincipal, authenticationScheme: this.Scheme.Name);
		return AuthenticateResult.Success(authenticationTicket);
	}
}