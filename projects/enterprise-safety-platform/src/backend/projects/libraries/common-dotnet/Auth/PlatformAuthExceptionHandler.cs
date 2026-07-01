using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Platform.Common.Auth;

/// <summary>
/// Exception handler for our authentication exceptions
/// </summary>
public class PlatformAuthExceptionHandler : IExceptionHandler
{
	/// <inheritdoc />
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext, 
		Exception exception, 
		CancellationToken cancellationToken)
	{
		if (exception is PlatformUnauthenticatedException)
		{
			await httpContext.Response.SendUnauthorizedAsync(cancellationToken);
			return true;
		}
		
		if (exception is PlatformUnauthorizedException)
		{
			await httpContext.Response.SendForbiddenAsync(cancellationToken);
			return true;
		}

		return false;
	}
}