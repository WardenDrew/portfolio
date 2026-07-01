using System.ComponentModel.DataAnnotations;

namespace SESAggregator.Configuration;

public class DatabaseSettings : IEnvSettings
{
	[Required]
	public static string MYSQL_SERVER => Environment.GetEnvironmentVariable(nameof(MYSQL_SERVER)) ?? string.Empty;

	[Required]
	public static uint MYSQL_PORT => Convert.ToUInt32(Environment.GetEnvironmentVariable(nameof(MYSQL_PORT)));

	[Required]
	public static string MYSQL_DATABASE => Environment.GetEnvironmentVariable(nameof(MYSQL_DATABASE)) ?? string.Empty;

	[Required]
	public static string MYSQL_USERNAME => Environment.GetEnvironmentVariable(nameof(MYSQL_USERNAME)) ?? string.Empty;

	[Required]
	public static string MYSQL_PASSWORD => Environment.GetEnvironmentVariable(nameof(MYSQL_PASSWORD)) ?? string.Empty;
}
