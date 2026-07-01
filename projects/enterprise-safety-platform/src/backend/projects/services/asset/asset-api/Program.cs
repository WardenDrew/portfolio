using System.Net;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Common.Extensions;
using Platform.Asset.Api.Providers;
using Platform.Common.Configuration.Providers;
using Platform.Common.Configuration.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Use JSONC appsettings
builder.UseAppsettingsJsonC();

// Logging Configuration
builder.Logging.ClearProviders()
	.AddConsole();

// Logger for program
ILogger<Program> logger = LoggerFactory
	.Create(programLogger => programLogger.AddConsole())
	.CreateLogger<Program>();

// Get settings here for setup
AssetsSettings? assetsSettings = builder.Configuration
	.GetSection(AssetsSettings.CONFIGURATION_KEY)
	.Get<AssetsSettings>();

if (assetsSettings is null)
{
	logger.LogError($"{AssetsSettings.CONFIGURATION_KEY} Not Configured");
	return -1;
}

if (assetsSettings.Enabled != true)
{
	logger.LogWarning("Disabled by configuration");
	return 0;
}

switch (assetsSettings.Provider)
{
	case AssetProvider.AWS_S3:
		logger.LogInformation("Using AWS S3 provider");
		builder.Services.AddAWSService<IAmazonS3>();
		break;
	case AssetProvider.LOCAL_DISK:
		logger.LogInformation("Using LOCAL DISK provider");
		builder.Services.AddHttpLogging(o => { });
		builder.Services.AddCors(cors =>
		{
			cors.AddDefaultPolicy(pol =>
			{
				pol.AllowAnyHeader()
					.AllowAnyOrigin()
					.AllowAnyMethod();
			});
		});
		break;
	default:
		logger.LogError("Unsupported provider");
		break;
}

// gRPC Service
builder.WebHost.ConfigureKestrel(options =>
{
	logger.LogInformation("Listening on 8080 with HTTP/2 for GRPC");
	options.Listen(
		address: IPAddress.Any, 
		port: 8080, 
		listenOptions =>
		{
			listenOptions.Protocols = HttpProtocols.Http2;
		});

	if (assetsSettings.Provider == AssetProvider.LOCAL_DISK)
	{
		logger.LogInformation("Listening on 8081 with HTTP/1.1 for Local Disk Provider API");
		options.Listen(
			address: IPAddress.Any, 
			port: 8081, 
			listenOptions =>
			{
				listenOptions.Protocols = HttpProtocols.Http1;
			});
	}
});

builder.Services.AddGrpc();

// Build App
WebApplication app = builder.Build();

// Map endpoints
switch (assetsSettings.Provider)
{
	case AssetProvider.AWS_S3:
		logger.LogInformation("Mapping the AWS S3 Provider GRPC API Implementation");
		app.MapGrpcService<AwsS3Provider>();
		break;
	case AssetProvider.LOCAL_DISK:
		app.UseRouting();
		app.UseCors();
		
		app.UseHttpLogging();

		logger.LogInformation("Mapping the LOCAL DISK Provider GRPC API Implementation");
		app.MapGrpcService<LocalDiskProvider>();
		
		logger.LogInformation("Mapping the LOCAL DISK Download Endpoint");
		app.MapGet("/request/{key}", (string key) =>
		{
			logger.LogInformation("Download Request {key}", key);
			return Results.File(
				path: LocalDiskProvider.GetFilePath(key, assetsSettings.Folder),
				contentType: "application/octet-stream");
		});
		
		logger.LogInformation("Mapping the LOCAL DISK Upload Endpoint");
		app.MapPut("/request/{key}", async (string key, Stream body) =>
		{
			logger.LogInformation("Upload Request {key}", key);
			await using FileStream fileStream = File.OpenWrite(
				LocalDiskProvider.GetFilePath(key, assetsSettings.Folder));

			await body.CopyToAsync(fileStream);

			return Results.NoContent();
		});
		break;
	default:
		logger.LogError("Unsupported provider");
		break;
}

// Run App
try
{
	await app.RunAsync();
}
catch (Exception ex)
{
	logger.LogError(exception: ex, "asset-api crashed!");
	return -1;
}

return 0;