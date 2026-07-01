namespace Platform.Legacy.Api.Middleware;

/// <summary>
/// This exists as the frontend sends "Content-Type: application/json" with GET requests
/// This is invalid, and while MVC Controllers lets it slide, FastEndpoints and MS Minimal API's do not and
/// reject it with a 415 code since a body was not included. We're going to rewrite this header if we see it on GET requests
/// as a shim since our mobile app can't update very frequently.
/// </summary>
public class GetMethodContentTypeRewriteMiddleware(RequestDelegate next)
{
	/// <summary>
	/// Middleware Entry Point
	/// </summary>
	/// <param name="context"></param>
	public async Task InvokeAsync(HttpContext context)
	{
		if (
			context.Request.Method.Equals(value: "GET", comparisonType: StringComparison.OrdinalIgnoreCase)
			&& context.Request.ContentType is not null
			&& context.Request.ContentType.Equals(value: "application/json", comparisonType: StringComparison.OrdinalIgnoreCase)
		)
		{
			context.Request.ContentType = null;
			context.Request.ContentLength = 0;
		}

		await next(context);
	}
}
