using System.ComponentModel.DataAnnotations;

namespace SESAggregator.Configuration;

public class SESSettings : IEnvSettings
{
	[Required]
	public static string ACCESS_KEY => Environment.GetEnvironmentVariable(nameof(ACCESS_KEY)) ?? string.Empty;

	[Required]
	public static string SECRET_KEY => Environment.GetEnvironmentVariable(nameof(SECRET_KEY)) ?? string.Empty;

	[Required]
	public static string FROM_ADDRESS => Environment.GetEnvironmentVariable(nameof(FROM_ADDRESS)) ?? string.Empty;

	[Required]
	public static string FROM_NAME => Environment.GetEnvironmentVariable(nameof(FROM_NAME)) ?? string.Empty;
}
