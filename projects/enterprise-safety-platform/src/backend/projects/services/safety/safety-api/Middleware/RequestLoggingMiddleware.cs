using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using Platform.Legacy.Core.Models.API;
using ILogger = NLog.ILogger;
using LogLevel = NLog.LogLevel;

namespace Platform.Legacy.Api.Middleware;

/// <summary>
/// Request Logging Middleware
/// </summary>
public static class RequestLoggingMiddleware
{
	/// <summary>
	/// Enable request logging
	/// </summary>
	/// <param name="app"></param>
	/// <param name="tracing"></param>
	public static void UseRequestLogging(this IApplicationBuilder app, bool tracing)
	{
		// Store the body as a property to the context so we can get it later
		_ = app.Use(
			async (context, next) =>
			{
				MemoryStream requestBodyStream = new();
				await context.Request.Body.CopyToAsync(requestBodyStream);

				_ = requestBodyStream.Seek(offset: 0, loc: SeekOrigin.Begin);

				string body = await new StreamReader(requestBodyStream).ReadToEndAsync();
				context.Items["body"] = body;
				_ = requestBodyStream.Seek(offset: 0, loc: SeekOrigin.Begin);
				context.Request.Body = requestBodyStream;

				await next();
			}
		);

		_ = app.UseExceptionHandler(handler => handler.Run(async context => await RequestLoggingMiddleware.LogRequestExceptionHandler(context)));

		if (tracing)
		{
			_ = app.UseMiddleware<TraceMiddleware>();
		}
	}

	/// <summary>
	/// Tracer
	/// </summary>
	public class TraceMiddleware
	{
		private readonly RequestDelegate _next;

		/// <summary>
		/// Constructor for Tracer
		/// </summary>
		/// <param name="next"></param>
		public TraceMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		/// <summary>
		/// Invoker
		/// </summary>
		/// <param name="context"></param>
		public async Task Invoke(HttpContext context)
		{
			await _next(context);

			Exception? ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
			if (ex is not null)
			{
				// This was already being handled by the Exception handler then
				return;
			}

			ILogger logger = LogManager.GetCurrentClassLogger();

			string message = "Trace Request Logging";
			LogEntry logEntry = RequestLoggingMiddleware.BuildLogEntry(context);

			LogEventInfo logEventInfo = new() { Message = message, Level = LogLevel.Trace, };
			logEventInfo.Properties.Add(key: "message", value: message);
			logEventInfo.Properties.Add(key: "details", value: logEntry);

			logger.Log(logEventInfo);
		}
	}

	private static async Task LogRequestExceptionHandler(HttpContext context)
	{
		ILogger logger = LogManager.GetCurrentClassLogger();

		try
		{
			Exception? ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

			if (System.Diagnostics.Debugger.IsAttached)
			{
				System.Diagnostics.Debugger.Break();
			}

			if (ex is null)
			{
				throw new NullReferenceException("Could not get the IExceptionHandlerFeature.Error");
			}

			IResponse response = RequestLoggingMiddleware.MatchExceptionResponse(ex: ex, context: context, logger: logger);

			context.Response.ContentType = "application/json";
			context.Response.StatusCode = response.CalculateStatusCode();

			await context.Response.WriteAsync(
				response.Serialize(
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
		}
		catch (Exception ex)
		{
			logger.Log(level: LogLevel.Fatal, message: $"There was an exception in the exception handler middleware: {ex.Message}");
		}
	}

	private static IResponse MatchExceptionResponse(Exception ex, HttpContext context, ILogger logger)
	{
		// ArgumentNullException is thrown if the request model is null or just flat out doesn't match the endpoint's request model
		if (ex is ArgumentNullException && ex.Source == "MediatR")
		{
			return Response.FromError(Core.Enums.ErrorCodes.Request.BAD_REQUEST_MODEL);
		}

		return RequestLoggingMiddleware.BuildUnhandledExceptionResponse(ex: ex, context: context, logger: logger);
	}

	private static LogEntry BuildLogEntry(HttpContext context)
	{
		LogEntry logEntry = new()
		{
			RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
			RemotePort = context.Connection.RemotePort,
			LocalIpAddress = context.Connection.LocalIpAddress?.ToString(),
			LocalPort = context.Connection.LocalPort,
			Protocol = context.Request.Protocol,
			Method = context.Request.Method,
			Scheme = context.Request.Scheme,
			Host = context.Request.Host.ToString(),
			Path = context.Request.Path.ToString(),
			Query = context.Request.QueryString.ToString(),
		};

		List<string> omitBodyPaths =
		[
			"/public/auth/authenticate",
			"/public/auth/refresh-token",
			"/public/auth/revoke-token",
			"/public/auth/confirm-email",
			"/public/auth/resend-confirm",
			"/public/auth/forgot-password",
			"/public/auth/reset-password",
			"/user/auth/change-password",
			"/admin/user/password/change",
			"/admin/user/password/reset",
		]; // Assembly scan this later on

		if (context.Items.TryGetValue(key: "body", value: out object? body) && body is string stringBody)
		{
			if (omitBodyPaths.Contains(context.Request.Path.ToString().ToLower()))
			{
				logEntry.Body = "****** SECRET REMOVED ******";
			}
			else
			{
				logEntry.Body = stringBody;
			}
		}

		List<string> secretHeaders = ["authorization", "x-api-key",];

		foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in context.Request.Headers)
		{
			if (secretHeaders.Contains(header.Key.ToLower()))
			{
				logEntry.Headers.Add(key: header.Key, value: "****** SECRET REMOVED ******");
				continue;
			}

			foreach (string? value in header.Value)
			{
				if (value is null)
				{
					continue;
				}

				logEntry.Headers.Add(key: header.Key, value: value);
			}
		}

		return logEntry;
	}

	private static IResponse BuildUnhandledExceptionResponse(Exception ex, HttpContext context, ILogger logger)
	{
		string message = ex.Message;
		List<Exception> exceptions = [ex,];

		while (ex.InnerException is not null)
		{
			exceptions.Add(ex.InnerException);
			ex = ex.InnerException;
		}

		LogEntry logEntry = RequestLoggingMiddleware.BuildLogEntry(context);

		foreach (Exception exc in exceptions)
		{
			LogEntry.LogEntryException logEntryException = new()
			{
				Message = exc.Message,
				ExceptionType = exc.GetType().FullName,
				Source = exc.Source,
				StackTrace = exc.StackTrace,
			};

			logEntry.Exceptions.Add(logEntryException);
		}

		LogEventInfo logEventInfo = new() { Message = message, Level = LogLevel.Fatal, };
		logEventInfo.Properties.Add(key: "message", value: message);
		logEventInfo.Properties.Add(key: "details", value: logEntry);

		logger.Log(logEventInfo);

		return Response.FromError(Core.Enums.ErrorCodes.Internal.EXCEPTION).WithData(logEntry);
	}

	/// <summary>
	/// A Log Entry for the remote logger
	/// </summary>
	public class LogEntry
	{
		/// <summary>
		/// Unique Guid of this entry
		/// </summary>
		public Guid Id { get; init; }

		/// <summary>
		/// The detected remote IP address of the request
		/// </summary>
		public string? RemoteIpAddress { get; set; }

		/// <summary>
		/// The detected remote port of the request
		/// </summary>
		public int RemotePort { get; set; }

		/// <summary>
		/// The detected local ip address of the server
		/// </summary>
		public string? LocalIpAddress { get; set; }

		/// <summary>
		/// The detected local port of the server
		/// </summary>
		public int LocalPort { get; set; }

		/// <summary>
		/// The protocol in use
		/// </summary>
		public string? Protocol { get; set; }

		/// <summary>
		/// The method in use
		/// </summary>
		public string? Method { get; set; }

		/// <summary>
		/// The scheme in use
		/// </summary>
		public string? Scheme { get; set; }

		/// <summary>
		/// The requested hostname
		/// </summary>
		public string? Host { get; set; }

		/// <summary>
		/// The requested path
		/// </summary>
		public string? Path { get; set; }

		/// <summary>
		/// The provided query params
		/// </summary>
		public string? Query { get; set; }

		/// <summary>
		/// The provided body content
		/// </summary>
		public string? Body { get; set; }

		/// <summary>
		/// The sent headers
		/// </summary>
		public Dictionary<string, string> Headers { get; set; }

		/// <summary>
		/// Any exceptions that occured
		/// </summary>
		public List<LogEntryException> Exceptions { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public LogEntry()
		{
			Id = Guid.NewGuid();
			Headers = new Dictionary<string, string>();
			Exceptions = [];
		}

		/// <summary>
		/// Model of an exception for the log entries
		/// </summary>
		public class LogEntryException
		{
			/// <summary>
			/// The Exception message
			/// </summary>
			public string? Message { get; set; }

			/// <summary>
			/// The Exception type
			/// </summary>
			public string? ExceptionType { get; set; }

			/// <summary>
			/// The source of the exception
			/// </summary>
			public string? Source { get; set; }

			/// <summary>
			/// The stacktrace of the exception
			/// </summary>
			public string? StackTrace { get; set; }
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return JsonConvert.SerializeObject(value: this, formatting: Formatting.Indented);
		}
	}
}
