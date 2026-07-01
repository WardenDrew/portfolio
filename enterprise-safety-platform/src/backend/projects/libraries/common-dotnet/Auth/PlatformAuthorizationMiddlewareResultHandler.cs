using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Platform.Common.Auth;

/// <summary>
/// 
/// </summary>
public class PlatformAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
#pragma warning disable CA1859
	private readonly IAuthorizationMiddlewareResultHandler handler;
#pragma warning restore CA1859

	/// <summary>
	/// Constructor
	/// </summary>
	public PlatformAuthorizationMiddlewareResultHandler()
	{
		handler = new AuthorizationMiddlewareResultHandler();
	}

	/// <summary>
	/// Handler
	/// </summary>
	/// <param name="next"></param>
	/// <param name="context"></param>
	/// <param name="policy"></param>
	/// <param name="authorizeResult"></param>
	public async Task HandleAsync(
		RequestDelegate next,
		HttpContext context,
		AuthorizationPolicy policy,
		PolicyAuthorizationResult authorizeResult
	)
	{
		if (authorizeResult.Challenged)
		{
			context.Response.StatusCode = 401;
			context.Response.Headers.WWWAuthenticate = new StringValues([
				"Bearer realm=\"example.com\"",
				$"cookie realm=\"example.com\" cookie-name=\"{EndpointAuthService.ACCESS_TOKEN_COOKIE_KEY}\"",
			]);

			/*context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(
				Response
					.FromError(Core.Enums.ErrorCodes.Authentication.MISSING_OR_EXPIRED_ACCESS_TOKEN)
					.Serialize(
						new JsonSerializerSettings
						{
							ContractResolver = new DefaultContractResolver()
							{
								NamingStrategy = new CamelCaseNamingStrategy(),
							},
							DateTimeZoneHandling = DateTimeZoneHandling.Utc,
						}
					)
			);*/

			await context.Response.CompleteAsync();
			return;
		}

		if (authorizeResult.Forbidden)
		{
			context.Response.StatusCode = 403;

			/*context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(
				Response
					.FromError(Core.Enums.ErrorCodes.Authorization.UNAUTHORIZED)
					.Serialize(
						new JsonSerializerSettings
						{
							ContractResolver = new DefaultContractResolver()
							{
								NamingStrategy = new CamelCaseNamingStrategy(),
							},
							DateTimeZoneHandling = DateTimeZoneHandling.Utc,
						}
					)
			);*/
			
			await context.Response.CompleteAsync();
			return;
		}

		await handler.HandleAsync(next: next, context: context, policy: policy, authorizeResult: authorizeResult);
	}
}
