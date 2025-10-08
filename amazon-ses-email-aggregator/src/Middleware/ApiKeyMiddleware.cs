using SESAggregator.Configuration;
using System.Net;
using System.Runtime.InteropServices;

namespace SESAggregator.Middleware;

public class ApiKeyMiddleware
{
	private readonly RequestDelegate next;
	private const string API_KEY_HEADER = "X-API-KEY";
	public const string API_APPLICATION_HEADER = "X-API-APPLICATION";

	public ApiKeyMiddleware(RequestDelegate next)
	{
		this.next = next;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Short circuit Options requests
		if (context.Request.Method == HttpMethods.Options)
		{
			context.Response.StatusCode = 200;
			context.Response.ContentType = "text/plan";
			context.Response.ContentLength = 0;
			return;
		}

		// Ensure we have the Header
		if (!context.Request.Headers.TryGetValue(
			API_KEY_HEADER, 
			out Microsoft.Extensions.Primitives.StringValues apiKeyValues))
		{
			context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			return;
		}

		// Ensure the api key is a valid string
		string apiKey = apiKeyValues.ToString();
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			return;
		}

		// Make sure this key is valid
		if (!ApiKeySettings.TryGetValue(apiKey, out string? keyUser) || keyUser is null)
		{
			context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			return;
		}

		context.Request.Headers.Add(API_APPLICATION_HEADER, keyUser);

		await next(context);
	}
}
