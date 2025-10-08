using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SESAggregator.Configuration;

public class ApiKeySettings : IEnvSettings
{
	[Required]
	public static string API_KEY_FILE => Environment.GetEnvironmentVariable(nameof(API_KEY_FILE)) ?? string.Empty;

	private static Dictionary<string, string> apiKeys = new();
	private static bool isInitialized = false;



	public static async Task Initialize(CancellationToken cancellationToken = default)
	{
		if (!File.Exists(API_KEY_FILE))
		{
			throw new FileNotFoundException();
		}

		string jsonFileContent = await File.ReadAllTextAsync(API_KEY_FILE, cancellationToken);

		if (string.IsNullOrWhiteSpace(jsonFileContent))
		{
			throw new InvalidOperationException("Api Key file is empty!");
		}

		Dictionary<string, string>? flippedKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonFileContent);
		if (flippedKeys is null || flippedKeys.Count == 0)
		{
			throw new InvalidOperationException("Api Key file has no keys!");
		}

		foreach (KeyValuePair<string, string> kvp in flippedKeys)
		{
			apiKeys.Add(kvp.Value, kvp.Key); // flipping here
		}

		isInitialized = true;
	}

	public static bool TryGetValue(string key, out string? value)
	{
		if (!isInitialized)
		{
			throw new InvalidOperationException("ApiKeySettings has not been initialized yet!");
		} 
		
		return apiKeys.TryGetValue(key, out value);
	}
}
