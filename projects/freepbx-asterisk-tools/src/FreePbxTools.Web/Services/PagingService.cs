using FreePbxTools.Common;

namespace FreePbxTools.Web.Services;

public class PagingService(
	ILogger<PagingService> logger,
	SettingsService settings)
{
	public async Task PageAsync(
		string groupExtension, 
		CancellationToken cancellationToken = default)
	{
		await using AmiClient client = new();

		await client.ConnectAsync(
			settings.Running.AsteriskHost, 
			settings.Running.AsteriskAmiPort, 
			cancellationToken);
		await client.LoginAsync(
			settings.Running.AsteriskAmiUsername,
			settings.Running.AsteriskAmiSecret, 
			cancellationToken);

		await client.SendMessageValidateResponseAsync(
			new()
			{
				["Action"] = "Originate",
				["Channel"] = $"Local/{groupExtension}@from-internal",
				["Application"] = "Hangup"
			},
			cancellationToken: cancellationToken
		);

		await client.LogoffAsync(cancellationToken);
	}
}