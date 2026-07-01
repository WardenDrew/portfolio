using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmtpServer;

namespace VoicemailEmailTranscription.Server;

public class SmtpBackgroundService(
	IServiceProvider serviceProvider,
	ILogger<SmtpBackgroundService> logger) 
	: BackgroundService
{
	protected override async Task ExecuteAsync(
		CancellationToken stoppingToken)
	{
		logger.LogInformation("Preparing SMTP Server");
		await using AsyncServiceScope scope 
			= serviceProvider.CreateAsyncScope();
		
		ISmtpServerOptions smtpServerOptions 
			= new SmtpServerOptionsBuilder()
				.ServerName("Voicemail Email Transcription")
				.Port(2525)
				.Build();
		
		SmtpServer.SmtpServer server
			= new SmtpServer.SmtpServer(
				options: smtpServerOptions, 
				serviceProvider: scope.ServiceProvider);
		
		logger.LogInformation("Starting SMTP Server");
		await server.StartAsync(stoppingToken);
		logger.LogInformation("SMTP Server Stopped");
	}
}
