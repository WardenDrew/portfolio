using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Common.Configuration;
using Platform.Common.Configuration.Providers;
using Platform.Common.Configuration.Services;
using Platform.Common.Encoding;
using Platform.Legacy.Core.Extensions.ServiceScanning;
using Stripe.Tax;

namespace Platform.Legacy.Core.Services;

public interface IPushLifetimeService : IServiceScanningServiceInterface
{
	FirebaseApp? Firebase { get; }
}

public class PushLifetimeService : IPushLifetimeService,
	IServiceScanningSingletonImplementation
{
	public FirebaseApp? Firebase { get; private set; }

	public PushLifetimeService(IConfiguration configuration, ILogger<PushLifetimeService> logger)
	{
		PushSettings? pushSettings = configuration
			.GetSection(PushSettings.CONFIGURATION_KEY)
			.Get<PushSettings>();

		if (pushSettings?.Enabled != true)
		{
			logger.LogWarning("Push is not enabled, skipping Push setup");
			return;
		}

		if (pushSettings.Provider != PushProvider.GOOGLE_FIREBASE)
		{
			logger.LogWarning("Unsupported or missing Push provider, skipping Push setup");
			return;
		}

		if (pushSettings.GoogleFirebase is null
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.Type)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.ProjectId)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.PrivateKeyId)
			|| pushSettings.GoogleFirebase.PrivateKey is null
			|| pushSettings.GoogleFirebase.PrivateKey.Length == 0
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.ClientEmail)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.ClientId)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.AuthUri)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.TokenUri)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.AuthProviderX509CertUrl)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.ClientX509CertUrl)
			|| string.IsNullOrWhiteSpace(pushSettings.GoogleFirebase.UniverseDomain)
			)
		{
			logger.LogError("Bad Google Firebase Credentials in config!");
			throw new InvalidOperationException("Bad firebase configuration");
		}

		string privateKey = string.Join(
			separator: string.Empty,
			values: pushSettings.GoogleFirebase.PrivateKey.AsEnumerable());

		if (string.IsNullOrWhiteSpace(privateKey))
		{
			logger.LogError("Malformed firebase privateKey, skipping Push setup");
			throw new InvalidOperationException("Bad firebase configuration");
		}

		this.Firebase = FirebaseApp.Create(
			new AppOptions()
			{
				/*Credential = Google.Apis.Auth.OAuth2.GoogleCredential
					.FromJson(key),*/
				Credential = Google.Apis.Auth.OAuth2.GoogleCredential
					.FromJsonParameters(new JsonCredentialParameters()
					{
						Type = pushSettings.GoogleFirebase.Type,
						ProjectId = pushSettings.GoogleFirebase.ProjectId,
						PrivateKeyId = pushSettings.GoogleFirebase.PrivateKeyId,
						PrivateKey = privateKey,
						ClientId =  pushSettings.GoogleFirebase.ClientId,
						ClientEmail = pushSettings.GoogleFirebase.ClientEmail,
						TokenUri = pushSettings.GoogleFirebase.TokenUri,
						UniverseDomain =  pushSettings.GoogleFirebase.UniverseDomain,
					}),
			}
		);
	}
}
