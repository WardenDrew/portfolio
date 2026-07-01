using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Platform.Common;
using Platform.Common.Configuration.Providers;
using Platform.Common.Configuration.Services;
using Platform.Protobuf.Asset;

namespace Platform.Asset.Api.Providers;

[SuppressMessage(
	"Performance",
	"CA1873:Avoid potentially expensive logging"
)]
public class LocalDiskProvider(
	ILogger<LocalDiskProvider> logger,
	IConfiguration configuration) 
	: AssetApi.AssetApiBase
{
	private readonly AssetsSettings? assetsSettings = configuration
		.GetSection(AssetsSettings.CONFIGURATION_KEY)
		.Get<AssetsSettings>();
		
	public static readonly string BasePath = Path.Join(
		Path.GetTempPath(),
		"Platform-Assets");
		
	public static string GetFolderPath(string? subdir)
	{
		string folderPath = Path.Join(LocalDiskProvider.BasePath, subdir);
		Directory.CreateDirectory(folderPath);
		return folderPath;
	}
	
	public static string GetFilePath(string id, string? subdir)
	{
		return Path.Join(LocalDiskProvider.GetFolderPath(subdir), id);
	}
	
	[MemberNotNullWhen(true, nameof(assetsSettings))]
	public bool Ready()
	{
		return assetsSettings != null
			&& assetsSettings.Enabled == true
			&& assetsSettings.Provider == AssetProvider.LOCAL_DISK
			&& !string.IsNullOrWhiteSpace(assetsSettings.Folder)
			&& !string.IsNullOrWhiteSpace(assetsSettings.ExternalEndpoint);
	}
	
	public override async Task<StatusResponse> Status(
		StatusRequest request,
		ServerCallContext context)
	{
		await Task.CompletedTask;
		
		return new StatusResponse
		{
			Ready = Ready(),
			Provider = nameof(AssetProvider.LOCAL_DISK).ToLower(),
			Location = GetFolderPath(assetsSettings?.Folder),
		};
	}
	
	private async Task<AssetRequest> BuildRequest(string id, string verb)
	{
		await Task.CompletedTask;
	
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}
		
		Instant expiresAt = Helpers.Now();
		
		return new AssetRequest
		{
			ExpiresAt = expiresAt.ToUnixTimeSeconds(),
			Method = verb,
			Uri = Path.Join([
				assetsSettings.ExternalEndpoint,
				"request",
				id,
			]),
			Body = string.Empty,
		};
	}

	public override async Task<DownloadRequestResponse> DownloadRequest(
		DownloadRequestRequest request,
		ServerCallContext context)
	{
		return new DownloadRequestResponse
		{
			Id = request.Id,
			Request = await BuildRequest(request.Id, "get"),
		};
	}

	public override async Task<UploadRequestResponse> UploadRequest(
		UploadRequestRequest request,
		ServerCallContext context)
	{
		return new UploadRequestResponse
		{
			Id = request.Id,
			Request = await BuildRequest(request.Id, "put"),
		};
	}
	
	public override async Task<DeleteResponse> Delete(
		DeleteRequest request,
		ServerCallContext context)
	{
		await Task.CompletedTask;
		
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}
		
		bool success = false;
		
		try
		{
			File.Delete(GetFilePath(request.Id, assetsSettings?.Folder));
			success = true;
		} 
		catch (Exception)
		{
			logger.LogWarning("Deletion of {id} failed!", request.Id);
		}
		
		return new DeleteResponse
		{
			Id = request.Id,
			Success = success,
		};
	}

	public override async Task<ListResponse> List(
		ListRequest request,
		ServerCallContext context
	)
	{
		await Task.CompletedTask;
		
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}
		
		ListResponse response = new();
		
		response.Ids.AddRange(
			Directory.EnumerateFiles(GetFolderPath(assetsSettings?.Folder)));
				
		return response;
	}

	public override async Task<ExistsResponse> Exists(
		ExistsRequest request,
		ServerCallContext context
	)
	{
		await Task.CompletedTask;
		
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}
		
		return new ExistsResponse
		{
			Id = request.Id,
			Exists = File.Exists(GetFilePath(request.Id, assetsSettings?.Folder)),
		};
	}
}