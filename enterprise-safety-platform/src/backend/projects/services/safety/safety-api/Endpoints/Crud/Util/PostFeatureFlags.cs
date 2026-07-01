using FastEndpoints;
using Platform.Legacy.Api.Services;
using Platform.Legacy.Core.Models.Auth;
using Platform.Legacy.Core.Services;

namespace Platform.Legacy.Api.Endpoints.Crud.Util;

internal class PostFeatureFlags(IAuthorizationService authorizationService, FeatureFlagService featureFlag)
	: Endpoint<List<string>, string>
{
	/// <inheritdoc/>
	public override void Configure()
	{
		Post("/util/featureflags");
	}

	/// <inheritdoc/>
	public override async Task HandleAsync(List<string> request, CancellationToken cancellationToken)
	{
		AccessToken? accessToken = authorizationService.ParseCurrentAccessToken();
		if (accessToken is null)
		{
			ThrowError(message: Core.Enums.ErrorCodes.Authentication.INVALID_ACCESS_TOKEN.EnglishTranslation, statusCode: 401);
			return;
		}

		if (!accessToken.IsDeveloper)
		{
			ThrowError(message: Core.Enums.ErrorCodes.Authorization.DEVELOPER_ONLY.EnglishTranslation, statusCode: 403);
			return;
		}

		string jwt = featureFlag.BuildFlags(request);

		await Send.OkAsync(response: jwt, cancellation: cancellationToken);
	}
}
