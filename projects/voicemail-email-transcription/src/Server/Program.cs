using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmtpServer.Storage;
using VoicemailEmailTranscription.Server;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSimpleConsole();
builder.Services.AddTransient<IMessageStore, ConsoleMessageStore>();
builder.Services.AddHostedService<SmtpBackgroundService>();

IHost app = builder.Build();
app.Run();