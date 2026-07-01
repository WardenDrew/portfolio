using System.Diagnostics.CodeAnalysis;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Platform.Common;
using Platform.Common.Configuration.Providers;
using Platform.Common.Configuration.Services;
using Platform.Protobuf.Asset;

namespace Platform.Asset.Api.Providers;

public class AwsS3Provider(
	ILogger<AwsS3Provider> logger,
	IConfiguration configuration,
	IAmazonS3 s3) 
	: AssetApi.AssetApiBase
{
	private readonly AssetsSettings? assetsSettings = configuration
		.GetSection(AssetsSettings.CONFIGURATION_KEY)
		.Get<AssetsSettings>();
	
	[MemberNotNullWhen(true, nameof(assetsSettings))]
	private bool Ready()
	{
		return assetsSettings != null
			&& assetsSettings.Enabled == true
			&& assetsSettings.Provider == AssetProvider.AWS_S3
			&& !string.IsNullOrWhiteSpace(assetsSettings.Bucket)
			&& !string.IsNullOrWhiteSpace(assetsSettings.Folder);
	}
	
	public override async Task<StatusResponse> Status(
		StatusRequest request,
		ServerCallContext context)
	{
		await Task.CompletedTask;
		
		return new StatusResponse
		{
			Ready = Ready(),
			Provider = nameof(AssetProvider.AWS_S3).ToLower(),
			Location = assetsSettings?.Bucket ?? string.Empty,
		};
	}
	
	private async Task<AssetRequest> BuildRequest(string id, HttpVerb verb)
	{
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}

		Instant expiresAt = Helpers.Now().Plus(Duration.FromMinutes(10));
		string key = Path.Join([
			assetsSettings.Folder,
			id,
		]);
		logger.LogInformation("Building Request {verb} {bucket} {key}", Enum.GetName(verb), assetsSettings.Bucket, key);

		try
		{
			string uri = await s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
			{
				BucketName = assetsSettings.Bucket,
				Key = key,
				Verb = verb,
				Expires = expiresAt.ToDateTimeUtc(),
			});

			return new AssetRequest
			{
				ExpiresAt = expiresAt.ToUnixTimeSeconds(),
				Method = verb.ToString().ToLower(),
				Uri = uri,
				Body = string.Empty,
			};
		}
		catch (AmazonS3Exception ex)
		{
			if (ex.StatusCode == HttpStatusCode.NotFound)
			{
				return new AssetRequest
				{
					ExpiresAt = 0,
					Method = string.Empty,
					Uri = string.Empty,
					Body = string.Empty,
				};
			}

			throw;
		}
	}

	public override async Task<DownloadRequestResponse> DownloadRequest(
		DownloadRequestRequest request,
		ServerCallContext context)
	{
		return new DownloadRequestResponse
		{
			Id = request.Id,
			Request = await BuildRequest(request.Id, HttpVerb.GET),
		};
	}

	public override async Task<UploadRequestResponse> UploadRequest(
		UploadRequestRequest request,
		ServerCallContext context)
	{
		return new UploadRequestResponse
		{
			Id = request.Id,
			Request = await BuildRequest(request.Id, HttpVerb.PUT),
		};
	}
	
	public override async Task<DeleteResponse> Delete(
		DeleteRequest request,
		ServerCallContext context)
	{
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}
		
		string key = Path.Join([
			assetsSettings.Folder,
			request.Id,
		]);
		logger.LogInformation("Deleting {bucket} {key}", assetsSettings.Bucket, key);

		try
		{
			DeleteObjectResponse deleteResult = await s3.DeleteObjectAsync(
				bucketName: assetsSettings.Bucket,
				key: key,
				cancellationToken: context.CancellationToken
			);

			return new DeleteResponse
			{
				Id = request.Id,
				Success = deleteResult.HttpStatusCode == System.Net.HttpStatusCode.NoContent,
			};
		}
		catch (AmazonS3Exception ex)
		{
			if (ex.StatusCode == HttpStatusCode.NotFound)
			{
				return new DeleteResponse
				{
					Id = request.Id,
					Success = true,
				};
			}
			
			return new DeleteResponse
			{
				Id = request.Id,
				Success = false,
			};
		}
	}

	public override async Task<ListResponse> List(
		ListRequest request,
		ServerCallContext context
	)
	{
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}
		
		ListObjectsV2Request listObjectsRequest = new()
		{
			BucketName = assetsSettings.Bucket,
			Prefix = assetsSettings.Folder,
			ContinuationToken = request.ContinuationToken,
		};

		ListObjectsV2Response listObjectsV2Response = await s3.ListObjectsV2Async(
			request: listObjectsRequest,
			cancellationToken: context.CancellationToken
		);
		
		ListResponse response = new()
		{
			ContinuationToken = listObjectsV2Response.NextContinuationToken,
		};
		
		response.Ids.AddRange(
			listObjectsV2Response
				.S3Objects.Select(x => x.Key));
				
		return response;
	}

	public override async Task<ExistsResponse> Exists(
		ExistsRequest request,
		ServerCallContext context
	)
	{
		if (!Ready())
		{
			logger.LogWarning("Not Ready");
			throw new RpcException(Grpc.Core.Status.DefaultCancelled);
		}
		
		string key = Path.Join([
			assetsSettings.Folder,
			request.Id,
		]);
		logger.LogInformation("Exists? {bucket} {key}", assetsSettings.Bucket, key);

		try
		{
			GetObjectMetadataResponse existsResponse = await s3.GetObjectMetadataAsync(
				bucketName: assetsSettings.Bucket,
				key: key,
				cancellationToken: context.CancellationToken
			);

			return new ExistsResponse
			{
				Id = request.Id,
				Exists = existsResponse.HttpStatusCode == System.Net.HttpStatusCode.OK,
			};
		}
		catch (AmazonS3Exception)
		{
			return new ExistsResponse
			{
				Id = request.Id,
				Exists = false,
			};
		}
	}
}