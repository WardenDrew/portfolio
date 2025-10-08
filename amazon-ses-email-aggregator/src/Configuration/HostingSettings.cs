using System.ComponentModel.DataAnnotations;

namespace SESAggregator.Configuration;

public class HostingSettings : IEnvSettings
{
	[Required]
	public static int LISTEN_PORT => Convert.ToInt32(Environment.GetEnvironmentVariable(nameof(LISTEN_PORT)));

	[Required]
	public static string HOSTNAME => Environment.GetEnvironmentVariable(nameof(HOSTNAME)) ?? string.Empty;
}
