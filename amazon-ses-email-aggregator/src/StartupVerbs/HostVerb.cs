using CommandLine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Serilog;
using SESAggregator.Configuration;
using SESAggregator.Data;
using SESAggregator.Data.Entities;
using SESAggregator.Middleware;
using SESAggregator.Models.Api;
using SESAggregator.Services;

namespace SESAggregator.StartupVerbs;

[Verb(
	name: "host",
	isDefault: true,
	HelpText = "Host a Kestrel Server for the API")]
public class HostVerb : IVerb
{
	[Option(
		longName: "env",
		HelpText = "Load a dotenv file into the environment variables before running")]
	public string? EnvPath { get; set; }

	[Option(
		longName: "debug",
		HelpText = "Log debug information")]
	public bool Debug { get; set; }

	public async Task<int> Handle(string[] args)
	{
		// Setup Logging
		LoggerConfiguration logConfig = new LoggerConfiguration()
			.WriteTo.Console();
		
		if (this.Debug)
		{
			logConfig.MinimumLevel.Debug();
		}
		else
		{
			logConfig.MinimumLevel.Information();
		}

		// This is the "Globally Shared" serilog logger
		Log.Logger = logConfig
			.CreateLogger();
		
		// Load Env File if provided
		if (!string.IsNullOrWhiteSpace(this.EnvPath))
		{
			EnvHelper.LoadENVFile(this.EnvPath);
		}

		// Validate Environment variables
		if (!EnvHelper.Validate(out List<string> missingEnvKeys, AssemblyMarker.Assembly))
		{
			Log.Fatal("Missing required environment variables: {MissingEnvironmentVariables}", missingEnvKeys);
			return -1;
		}

		// Initialize Api Key Settings
		await ApiKeySettings.Initialize();

		// Builder for the Application Container
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		// Use Serilog Logging
		_ = builder.Host.UseSerilog();

		// Kestrel server options
		_ = builder.WebHost.UseUrls(); // wipe urls to listen on prior to our direct listen binding's
		_ = builder.WebHost.UseKestrel(kestrelOpt =>
		{
			kestrelOpt.Listen(System.Net.IPAddress.Parse("0.0.0.0"), HostingSettings.LISTEN_PORT);
		});

		// Setup Database Context
		_ = AppDbContext.SetupServices(builder.Services);

		// Services
		_ = builder.Services.AddScoped<SESDispatchService>();
		_ = builder.Services.AddHostedService<Orchestrator>();

		// Build the Application Container
		WebApplication app = builder.Build();

		// Middleware
		_ = app
			.UseRouting()
			.UseMiddleware<ApiKeyMiddleware>();

		// Endpoints
		_ = app.MapPost("/message", async (
			AppDbContext db, 
			[FromHeader(Name = ApiKeyMiddleware.API_APPLICATION_HEADER)] string application,
			[FromBody] PostMessageBody body
			) =>
		{
			if (string.IsNullOrWhiteSpace(body.Address) ||
				!body.Address.Contains('@') ||
				string.IsNullOrWhiteSpace(body.Name) ||
				string.IsNullOrWhiteSpace(body.Subject) ||
				string.IsNullOrWhiteSpace(body.Body))
			{
				return Results.BadRequest();
			}
			
			Message message = new()
			{
				ToAddress = body.Address,
				ToName = body.Name,
				Subject = body.Subject,
				Body = body.Body,
				SendingApplication = application,
				QueuedAt = DateTime.UtcNow
			};
			_ = db.Add(message);
			_ = await db.SaveChangesAsync();

			return Results.Accepted($"/message/{message.Id}");
		});

		// Run the Application Container
		try
		{
			app.Run();
		}
		catch (TaskCanceledException) // This only gets hit on application shutdown if a hostedservice failed
		{
			return 1;
		}

		// We never should end up here but for the sake of completeness
		await Task.Delay(1);
		return 0;
	}
}
