using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Platform.Common.Auth;
using Platform.Common.Configuration;
using Platform.Common.Jwt;

namespace Platform.Common.Extensions;

/// <summary>
/// Extensions to other services and classes, commonly IServiceCollection for startup
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds services that API projects require to validate incoming requests
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddPlatformAuth(this IServiceCollection services)
	{
		services.AddSingleton<JwtSerializer>();
		
		services.AddExceptionHandler<PlatformAuthExceptionHandler>();
		
		services.AddScoped<EndpointAuthService>();
		
		services.AddAuthentication()
			.AddScheme<PlatformAuthenticationSchemeOptions, PlatformAuthenticationHandler>(
				authenticationScheme: "PlatformAuth",
				configureOptions: _ => { });

		services.AddAuthorization();
		
		services.AddSingleton<IAuthorizationMiddlewareResultHandler, PlatformAuthorizationMiddlewareResultHandler>();
		
		services.AddHttpContextAccessor();

		return services;
	}
}