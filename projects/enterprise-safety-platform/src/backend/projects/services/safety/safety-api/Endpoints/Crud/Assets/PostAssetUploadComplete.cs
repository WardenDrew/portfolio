using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Platform.Legacy.Core.Models.API;
using Platform.Legacy.Core.Models.Auth;
using Platform.Legacy.Core.Services;
using Platform.Legacy.Data;
using Platform.Legacy.Data.Entities.Assets;

namespace Platform.Legacy.Api.Endpoints.Crud.Assets;

/// <summary>
/// Asset upload complete endpoint
/// </summary>
/// <param name="db"></param>
/// <param name="authorizationService"></param>
internal class PostAssetUploadComplete(LegacyDbContext db, IAuthorizationService authorizationService)
	: Endpoint<PostAssetUploadComplete.RequestData, IResponse>
{
	/// <summary>
	/// Request Model
	/// </summary>
	public class RequestData
	{
		/// <summary>
		/// The ID of the Asset to get
		/// </summary>
		public int AssetId { get; set; }
	}

	/// <summary>
	/// Configuration method
	/// </summary>
	public override void Configure()
	{
		Verbs(Http.POST);
		Routes(
			"/admin/asset/upload/complete",
			"/user/asset/upload/complete",
			"/operator/asset/upload/complete",
			"/asset/upload/complete"
		);
		Description(
			builder: x =>
				x.WithTags("Assets")
					.Accepts<RequestData>("application/json")
					.Produces<IResponse>()
					.Produces<IResponse>(400)
					.Produces<IResponse>(401)
					.Produces<IResponse>(404),
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

		Asset? asset = await db.Set<Asset>()
			.Where(x => x.UploadedByUserId == accessToken.UserId)
			.Where(x => x.Id == request.AssetId)
			.FirstOrDefaultAsync(cancellationToken);

		if (asset is null)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(EntityErrorCodeProvider<Asset>.NOT_FOUND),
				statusCode: 404,
				cancellation: cancellationToken
			);
			return;
		}

		if (asset.UploadCompletedOn.HasValue)
		{
			await Send.ResponseAsync(
				response: Core.Models.API.Response.FromError(Asset.Errors.UPLOAD_COMPLETED_ALREADY),
				statusCode: 400,
				cancellation: cancellationToken
			);
			return;
		}

		asset.UploadCompletedOn = DateTime.UtcNow;
		_ = await db.SaveChangesAsync(cancellationToken);

		await Send.ResponseAsync(response: Core.Models.API.Response.FromSuccess(), statusCode: 200, cancellation: cancellationToken);
	}
}
