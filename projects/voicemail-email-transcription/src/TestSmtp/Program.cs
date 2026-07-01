using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;


MimeMessage message = new();
message.From.Add(new MailboxAddress("From User", "from@transcribe"));
message.To.Add(new MailboxAddress("To User","to@transcribe"));
message.Subject = "Transcribe Please";
message.Body = new TextPart("plain")
{
	Text = "Testing voicemail transcription",
};

SmtpClient client = new(new ProtocolLogger(Console.OpenStandardOutput()));
client.Connect("localhost", 2525, SecureSocketOptions.None);
client.Send(message);
client.Disconnect(true);