using System.Buffers;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace VoicemailEmailTranscription.Server;

public class ConsoleMessageStore(ILogger<ConsoleMessageStore> logger) : MessageStore
{
	public override async Task<SmtpResponse> SaveAsync(
		ISessionContext context,
		IMessageTransaction transaction,
		ReadOnlySequence<byte> buffer,
		CancellationToken cancellationToken
	)
	{
		try
		{
			await using MemoryStream stream = new MemoryStream();
			SequencePosition position = buffer.GetPosition(0);
			while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
			{
				await stream.WriteAsync(memory, cancellationToken);
			}
			stream.Position = 0;
			
			MimeMessage message 
				= await MimeMessage.LoadAsync(stream, cancellationToken);

			logger.LogInformation(
				"Message from {from} to {to}", 
				message.From.Cast<MailboxAddress>().Single().Address, 
				message.To.Cast<MailboxAddress>().Single().Address);

			return SmtpResponse.Ok;
		} catch (Exception ex)
		{
			logger.LogError(ex.Message);
			return SmtpResponse.TransactionFailed;
		}
	}
}