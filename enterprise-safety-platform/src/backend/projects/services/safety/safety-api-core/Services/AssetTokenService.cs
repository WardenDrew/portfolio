using System.Security.Claims;
using Platform.Common.Jwt;
using Platform.Legacy.Core.Extensions.ServiceScanning;
using Platform.Legacy.Core.Models.Auth;
using Platform.Legacy.Data.Entities.Assets;

namespace Platform.Legacy.Core.Services;

public interface IAssetTokenService : IServiceScanningServiceInterface
{
	string GenerateAssetToken(int assetId);
	AssetTokenValidationResponse ValidateAssetToken(string? assetToken);
	bool IsUserAccessPermitted(Asset asset, AccessToken accessToken, string? token);
}

public class AssetTokenService(JwtSerializer jwtSerializer)
	: IAssetTokenService, IServiceScanningScopedImplementation
{
	public string GenerateAssetToken(int assetId)
	{
		return jwtSerializer.Serialize(AssetToken.New(assetId));
	}

	public AssetTokenValidationResponse ValidateAssetToken(string? token)
	{
		AssetTokenValidationResponse response = new();

		if (string.IsNullOrWhiteSpace(token))
		{
			return response;
		}

		// Build the asset token model from the claims
		AssetToken assetToken;
		try
		{
			 assetToken = jwtSerializer.Deserialize<AssetToken>(token);
		}
		catch (Exception)
		{
			return response;
		}

		// Ensure we're not expired already
		if (assetToken.ExpiresOn <= DateTime.UtcNow)
		{
			return response;
		}

		response.Authorized = true;
		response.AssetId = assetToken.AssetId;

		return response;
	}

	public bool IsUserAccessPermitted(Asset asset, AccessToken accessToken, string? token)
	{
		if (asset.UploadedByUserId == accessToken.UserId)
		{
			return true;
		}

		if (asset.PermitPublic)
		{
			return true;
		}

		if (asset.PermitCompany && asset.CompanyId == accessToken.CompanyId)
		{
			return true;
		}

		AssetTokenValidationResponse assetTokenValidationResponse = this.ValidateAssetToken(token);

		if (assetTokenValidationResponse.Authorized && asset.Id == assetTokenValidationResponse.AssetId)
		{
			return true;
		}

		return false;
	}
}
