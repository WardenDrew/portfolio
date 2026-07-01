using CommandLine.Text;
using CommandLine;
using Serilog;
using SESAggregator.Configuration;
using SESAggregator.Data;
using Microsoft.EntityFrameworkCore;

namespace SESAggregator.StartupVerbs;

[Verb(
	name: "migrate",
	HelpText = "Runs a database migration if it is needed.")]
public class MigrateVerb : IVerb
{
	[Option(
		longName: "env",
		HelpText = "Load a dotenv file into the environment variables before running")]
	public string? EnvPath { get; set; }

	public async Task<int> Handle(string[] args)
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
			return -1;
		}

		// Create a dummy Service Collection
		IServiceCollection services = new ServiceCollection();

		// Setup Database Context
		_ = AppDbContext.SetupServices(services);

		// Build the service provider and get a db context from it
		AppDbContext db = services.BuildServiceProvider().GetRequiredService<AppDbContext>();
		await db.Database.MigrateAsync();

		return 0;
	}
}
