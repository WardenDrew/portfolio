using FastEndpoints;
using Platform.Common.Auth;

namespace Platform.Common.FastEndpoints;

/// <summary>
/// A FastEndpoints endpoint with additional services loaded from the dependency injection container by default
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public abstract class PlatformEndpoint<TRequest, TResponse> 
	: Endpoint<TRequest, TResponse> 
	where TRequest : notnull
{
	/*private LegacyDbContext? _db;
	/// <summary>
	/// The Legacy Db Context
	/// </summary>
	protected LegacyDbContext Db => _db ??= Resolve<LegacyDbContext>();*/
	
	private EndpointAuthService? _auth;
	/// <summary>
	/// The Endpoint Auth Service for permissions checks
	/// </summary>
	protected EndpointAuthService Auth => _auth ??= Resolve<EndpointAuthService>();
}