using FastEndpoints;
using FluentValidation;
using Grpc.Core;
using Grpc.Net.Client;
using Platform.Common.Configuration.Services;
using Platform.Legacy.Core.Models.API;
using Platform.Legacy.Core.Models.Auth;
using Platform.Legacy.Core.Services;
using Platform.Legacy.Data;
using Platform.Legacy.Data.Entities.Assets;
using Platform.Protobuf.Asset;

namespace Platform.Legacy.Api.Endpoints.Crud.Assets;

/// <summary>
/// Asset upload complete endpoint
/// </summary>
/// <param name="authorizationService"></param>
/// <param name="db"></param>
/// <param name="configuration"></param>
internal class PostAssetUploadBegin(
	LegacyDbContext db,
	IAuthorizationService authorizationService,
	IConfiguration configuration
) : Endpoint<PostAssetUploadBegin.RequestData, IResponse>
{
	/// <summary>
	/// Request Model
	/// </summary>
	public class RequestData
	{
		public required string Name { get; set; }
		public string? Description { get; set; }
		public string? OriginalFileName { get; set; }
		public required string MimeType { get; set; }
		public bool Cacheable { get; set; }
		public bool PermitCompany { get; set; }

		// Operator-only fields
		public bool? PermitPublic { get; set; }
		public int? OverrideCompanyId { get; set; }
	}

	/// <summary>
	/// Fluent Validation
	/// </summary>
	public class Validator : Validator<RequestData>
	{
		public Validator()
		{
			RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
			RuleFor(x => x.OriginalFileName).MaximumLength(255);
			RuleFor(x => x.MimeType).NotEmpty().MaximumLength(255);
		}
	}

	/// <summary>
	/// Response Model
	/// </summary>
	public class ResponseData
	{
		public int AssetId { get; set; }
		public Guid AssetGuid { get; set; }
		public required string UploadUrl { get; set; }
		public string? ThumbnailUploadUrl { get; set; }
	}

	/// <summary>
	/// Configuration method
	/// </summary>
	public override void Configure()
	{
		Verbs(Http.POST);
		Routes(
			"/admin/asset/upload/begin",
			"/user/asset/upload/begin",
			"/operator/asset/upload/begin",
			"/asset/upload/begin"
		);
		Description(x =>
			x.WithTags("Assets")
				.Accepts<RequestData>("application/json")
				.Produces<IResponse<ResponseData>>()
				.ProducesProblemFE(401)
				.ProducesProblemFE(403)
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
        
		Asset asset = new()
		{
			PublicId = Guid.NewGuid(),
			CompanyId =
				accessToken.IsAnyOperator && request.OverrideCompanyId.HasValue
					? request.OverrideCompanyId.GetValueOrDefault()
					: accessToken.CompanyId,
			UploadedByUserId = accessToken.UserId,
			Name = request.Name,
			Description = request.Description,
			OriginalFileName = request.OriginalFileName,
			MimeType = request.MimeType,
			Cacheable = request.Cacheable,
			PermitCompany = request.PermitCompany,
			PermitPublic = accessToken.IsAnyOperator && (request.PermitPublic ?? false),
			CreatedOn = DateTime.UtcNow,
		};

		_ = db.Add(asset);

		string? uploadUrl;
		string? thumbnailUploadUrl = null;

        try
        {
            using GrpcChannel channel = GrpcChannel
                .ForAddress(assetsSettings.InternalEndpoint);
            AssetApi.AssetApiClient assetApi = new(channel);

            uploadUrl = (await assetApi.UploadRequestAsync(
				request: new UploadRequestRequest
				{
					Id = asset.PublicId.ToString(),
				},
                cancellationToken: cancellationToken))
				.Request.Uri;

            if (request.MimeType.StartsWith(value: "image/", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
				thumbnailUploadUrl = (await assetApi.UploadRequestAsync(
					request: new UploadRequestRequest
					{
						Id = $"thumb-{asset.PublicId.ToString()}",
					},
					cancellationToken: cancellationToken))
					.Request.Uri;
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
		
		_ = await db.SaveChangesAsync(cancellationToken);
        
		await Send.ResponseAsync(
			response: Core.Models.API.Response.FromSuccess()
				.WithData(
					new ResponseData
					{
						AssetId = asset.Id,
						AssetGuid = asset.PublicId,
						UploadUrl = uploadUrl,
						ThumbnailUploadUrl = thumbnailUploadUrl,
					}
				),
			statusCode: 200,
			cancellation: cancellationToken
		);
	}
}
