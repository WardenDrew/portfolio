using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Platform.Legacy.Core.Models.API;

namespace Platform.Legacy.Api.Middleware;

/// <summary>
/// Authorization Middleware
/// </summary>
public class AuthorizeResultMiddleware : IAuthorizationMiddlewareResultHandler
{
	private readonly IAuthorizationMiddlewareResultHandler handler;

	/// <summary>
	/// Constructor
	/// </summary>
	public AuthorizeResultMiddleware()
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
			context.Response.ContentType = "application/json";
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
			);
			return;
		}

		if (authorizeResult.Forbidden)
		{
			context.Response.StatusCode = 403;
			context.Response.ContentType = "application/json";
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
			);
			return;
		}

		await handler.HandleAsync(next: next, context: context, policy: policy, authorizeResult: authorizeResult);
	}
}
