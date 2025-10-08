using CommandLine.Text;
using CommandLine;
using Microsoft.EntityFrameworkCore;
using SESAggregator.Data;
using Serilog;
using SESAggregator.Configuration;

namespace SESAggregator.StartupVerbs;


/// <summary>
/// This verb is called by the ./add-migration.sh script to build a minimal set of services for EF to detect the DbContext
/// </summary>
[Verb(
	name: "build-migrations",
	HelpText = "Configure the minimal pieces of the Application container for EF to extract the DbContext")]
public class BuildMigrationsVerb : IVerb
{
	[Option(
		longName: "env",
		HelpText = "Load a dotenv file into the environment variables before running")]
	public string? EnvPath { get; set; }

	[Option(
		longName: "applicationName",
		HelpText = "DotNet EF Migration Tools provide this automatically")]
	public string? ApplicationName { get; set; }

	public Task<int> Handle(string[] args)
	{
		// Load Env File if provided
		if (!string.IsNullOrWhiteSpace(this.EnvPath))
		{
			EnvHelper.LoadENVFile(this.EnvPath);
		}

		// Validate Environment variables
		if (!EnvHelper.Validate(out List<string> missingEnvKeys, AssemblyMarker.Assembly))
		{
			Log.Fatal("Missing required environment variables: {MissingEnvironmentVariables}", missingEnvKeys);
			return ValueTask.FromResult(-1).AsTask();
		}

		// Builder for the Application Container
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		// Setup Database Context
		_ = AppDbContext.SetupServices(builder.Services);

		// Build the Application Container
		WebApplication app = builder.Build();

		return ValueTask.FromResult(0).AsTask();
	}
}
