using FastEndpoints;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Platform.Common.Configuration.Services;
using Platform.Legacy.Core.Models.API;
using Platform.Legacy.Core.Models.Auth;
using Platform.Legacy.Core.Services;
using Platform.Legacy.Data;
using Platform.Legacy.Data.Entities.Assets;
using Platform.Protobuf.Asset;

namespace Platform.Legacy.Api.Endpoints.Crud.Assets;

/// <summary>
/// Fast endpoint for Asset Download
/// </summary>
/// <param name="authorizationService"></param>
/// <param name="db"></param>
/// <param name="configuration"></param>
/// <param name="assetTokenService"></param>
internal class PostDownloadAsset(
	IAuthorizationService authorizationService,
	LegacyDbContext db,
	IConfiguration configuration,
	IAssetTokenService assetTokenService
) : Endpoint<PostDownloadAsset.RequestData, IResponse>
{
	/// <summary>
	/// Request Model
	/// </summary>
	public class RequestData
	{
		/// <summary>
		/// The Document Asset Id to get the document.
		/// </summary>
		public int AssetId { get; set; }
		public string? Token { get; set; }
	}

	/// <summary>
	/// Configuration method
	/// </summary>
	public override void Configure()
	{
		Verbs(Http.POST);
		Routes("/admin/asset/download", "/user/asset/download", "/operator/asset/download", "/asset/download");
		Description(
			builder: x =>
				x.WithTags("Assets")
					.Accepts<RequestData>("application/json")
					.Produces<ResponseData>()
					.ProducesProblemFE()
					.ProducesProblemFE()
					.ProducesProblemFE(401)
					.ProducesProblemFE(404),
			clearDefaults: true
		);
	}

	/// <summary>
	/// Handler method
	/// </summary>
	/// <param name="request"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public override async Task HandleAsync(RequestData request, CancellationToken cancellationToken)
	{
		AccessToken? accessToken = authorizationService.ParseCurrentAccessToken();
		if (accessToken is null)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(Core.Enums.ErrorCodes.Authentication.INVALID_ACCESS_TOKEN),
				statusCode: 401,
				cancellation: cancellationToken
			);
			return;
		}

        AssetsSettings? assetsSettings = configuration
			.GetSection(AssetsSettings.CONFIGURATION_KEY)
			.Get<AssetsSettings>();

        if (assetsSettings?.Enabled != true ||
            assetsSettings.InternalEndpoint == null)
        {
			await Send.StatusCodeAsync(
				statusCode: 501, 
				cancellation: cancellationToken);
            return;
        }

        IQueryable<Asset> query = db.Set<Asset>();

        if (!accessToken.IsAnyOperator)
        {
            query = query.Where(x => x.CompanyId == accessToken.CompanyId);
        }

        Asset? asset = await query.Where(x => x.Id == request.AssetId).FirstOrDefaultAsync(cancellationToken);

        if (asset is null)
        {
            await Send.ResponseAsync(
                response: Core.Models.API.Response.FromError(EntityErrorCodeProvider<Asset>.NOT_FOUND),
                statusCode: 404,
                cancellation: cancellationToken
            );
            return;
        }

        if (!accessToken.IsAnyAdmin && !assetTokenService.IsUserAccessPermitted(asset: asset, accessToken: accessToken, token: request.Token))
        {
            await Send.ResponseAsync(
                response: Core.Models.API.Response.FromError(Core.Enums.ErrorCodes.Authorization.UNAUTHORIZED),
                statusCode: 403,
                cancellation: cancellationToken
            );
            return;
        }

		string? downloadUrl = null;
        string? thumbnailUrl = null;
        string? canvasUrl = null;
        string? canvasThumbUrl = null;

        try
        {
            using GrpcChannel channel = GrpcChannel
                .ForAddress(assetsSettings.InternalEndpoint);
            AssetApi.AssetApiClient assetApi = new(channel);

            downloadUrl = (await assetApi.DownloadRequestAsync(new DownloadRequestRequest { Id = $"{asset.PublicId.ToString()}" },
                cancellationToken: cancellationToken)).Request.Uri;

            if (asset.MimeType.StartsWith(value: "image/", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                bool thumbnailExists = (await assetApi.ExistsAsync(new ExistsRequest { Id = $"thumb-{asset.PublicId.ToString()}" },
                cancellationToken: cancellationToken)).Exists;

                if (thumbnailExists)
                {
                    thumbnailUrl = (await assetApi.DownloadRequestAsync(new DownloadRequestRequest { Id = $"thumb-{asset.PublicId.ToString()}" },
				cancellationToken: cancellationToken)).Request.Uri;
                }

                bool canvasExists = (await assetApi.ExistsAsync(new ExistsRequest { Id = $"canvas-{asset.PublicId.ToString()}" },
					cancellationToken: cancellationToken)).Exists;

                if (canvasExists)
                {
                    canvasUrl = (await assetApi.DownloadRequestAsync(new DownloadRequestRequest { Id = $"canvas-{asset.PublicId.ToString()}" },
                cancellationToken: cancellationToken)).Request.Uri;
                }

                bool canvasThumbExists = (await assetApi.ExistsAsync(new ExistsRequest { Id = $"canvasThumb-{asset.PublicId.ToString()}" },
					cancellationToken: cancellationToken)).Exists;

                if (canvasThumbExists)
                {
                    canvasThumbUrl = (await assetApi.DownloadRequestAsync(new DownloadRequestRequest { Id = $"canvasThumb-{asset.PublicId.ToString()}" },
                cancellationToken: cancellationToken)).Request.Uri;
                }
            }

        }
		catch (RpcException ex)
		{
			await Send.StringAsync(
				content: ex.Message,
				statusCode: 503, 
				cancellation: cancellationToken);
            return;
        }

		await Send.ResponseAsync(
			response: Core.Models.API.Response.FromSuccess()
				.WithData(
					new ResponseData()
					{
						PublicId = asset.PublicId,
						Cacheable = asset.Cacheable,
						OriginalFileName = asset.OriginalFileName,
						MimeType = asset.MimeType,
						DownloadUrl = downloadUrl,
						ThumbnailUrl = thumbnailUrl,
						CanvasUrl = canvasUrl,
						CanvasThumbUrl = canvasThumbUrl,
					}
				),
			statusCode: 200,
			cancellation: cancellationToken
		);
	}

	/// <summary>
	/// Response Model
	/// </summary>
	public class ResponseData
	{
		public Guid PublicId { get; set; }
		public bool Cacheable { get; set; }
		public string? OriginalFileName { get; set; }
		public required string MimeType { get; set; }
		public required string DownloadUrl { get; set; }
		public string? ThumbnailUrl { get; set; }
		public string? CanvasUrl { get; set; }
		public string? CanvasThumbUrl { get; set; }
	}
}
