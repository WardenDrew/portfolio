using CaptivePortal.Components;
using CaptivePortal.Database.Entities;
using CaptivePortal.Database;
using LettuceEncrypt.Acme;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using CaptivePortal.Services.Outer;
using CaptivePortal.Services.Dns;

namespace CaptivePortal.Daemons
{
    public class WebDaemon(
        IronNacConfiguration configuration,
        ILogger<WebDaemon> logger,
        AppArgsService appArgsService,
        IServiceProvider outerServiceProvider) 
        : BaseDaemon<WebDaemon>(logger)
    {
        protected override async Task EntryPoint(CancellationToken cancellationToken)
        {
            try
            {
                if (configuration.WebUseHttps &&
                    !configuration.AllHttpsRequirementsMet)
                {
                    logger.LogWarning("HTTPS is enabled, however not all of the HTTPS required environment variables are set! HTTPS will not be enabled!");
                }
                
                WebApplicationBuilder builder = WebApplication.CreateBuilder(appArgsService.Args);

                builder.WebHost.UseKestrel(kestrel =>
                {
                    foreach (string address in configuration.WebListenAddresses)
                    {
                        kestrel.Listen(IPAddress.Parse(address), configuration.WebHttpPort);

                        if (configuration.AllHttpsRequirementsMet)
                        {
                            kestrel.Listen(IPAddress.Parse(address), configuration.WebHttpsPort, listenOpts =>
                            {
                                listenOpts.UseHttps(https =>
                                {
                                    https.UseLettuceEncrypt(kestrel.ApplicationServices);
                                });
                            });
                        }
                    }
                });

                OuterServiceProviderService outerSpService = new(outerServiceProvider);
                outerSpService.RegisterServicesInChild(builder.Services);

                if (configuration.AllHttpsRequirementsMet)
                {
                    builder.Services.AddLettuceEncrypt(async opts =>
                    {
                        opts.AcceptTermsOfService = true;
                        opts.AllowedChallengeTypes = ChallengeType.Dns01;
                        opts.DomainNames = [configuration.WebHostname];
                        opts.EmailAddress = configuration.WebHttpsCertEmail;
                        
                        if (opts.UseStagingServer)
                        {
                            Logger.LogInformation("Using the Lets Encrypt staging environment. Checking for stage root certs!");

                            List<string> stageRootCertUris = builder.Configuration.GetSection("LetsEncrypt:StageRootCerts")
                                .Get<string[]>()
                                ?.ToList()
                                ?? throw new MissingFieldException("LetsEncrypt:StageRootCerts");
                            List<string> stageRootCertContents = new();

                            using HttpClient issuersHttpClient = new();

                            foreach (string stageRootCertUri in stageRootCertUris)
                            {
                                Logger.LogInformation("Downloading Lets Encrypt Staging Certificate: {uri}", stageRootCertUri);
                                stageRootCertContents.Add(await issuersHttpClient.GetStringAsync(stageRootCertUri));
                            }

                            opts.AdditionalIssuers = stageRootCertContents.ToArray();
                        }
                    });

                    builder.Services.Replace(
                        new ServiceDescriptor(
                            typeof(IDnsChallengeProvider),
                            typeof(PublicDnsChallengeProvider),
                            ServiceLifetime.Singleton));
                }

                builder.Services.AddRazorComponents()
                    .AddInteractiveServerComponents();

                builder.Services.AddHttpContextAccessor();

                WebApplication app = builder.Build();

                List<string> hostWhitelist = new();
                hostWhitelist.AddRange(configuration.WebListenAddresses);
                hostWhitelist.AddRange(configuration.WebRedirectBypassHosts);
                if (!string.IsNullOrWhiteSpace(configuration.WebHostname))
                    hostWhitelist.Add(configuration.WebHostname);

                app.Use(async (context, next) =>
                {
                    // If we're not on the host whitelist, redirect to the captive portal
                    if (!hostWhitelist.Where(x => x.Equals(context.Request.Host.Host, StringComparison.InvariantCultureIgnoreCase)).Any())
                    {
                        context.Response.StatusCode = 302;
                        context.Response.Headers["Location"] 
                            = $"http://{configuration.WebRedirectDestination}/portal?redirect={context.Request.GetEncodedUrl()}";
                        return;
                    }

                    if (configuration.AllHttpsRequirementsMet)
                    {
                        // If we're accessing via fqdn and not https, upgrade connection to https
                        if (context.Request.Scheme == "http" && context.Request.Host.Host == configuration.WebRedirectDestination)
                        {
                            string redirectLocation = context.Request.GetEncodedUrl();
                            redirectLocation = $"https{redirectLocation.AsSpan().Slice(4)}";
                            context.Response.StatusCode = 302;
                            context.Response.Headers["Location"] = redirectLocation;
                            return;
                        }
                    }

                    await next(context);
                });

                app.UseStaticFiles();
                app.UseAntiforgery();

                app.MapRazorComponents<App>()
                    .AddInteractiveServerRenderMode();

                app.UseStatusCodePagesWithRedirects("/{0}");

                Logger.LogInformation("{listener} started", nameof(WebDaemon));
                this.Running = true;

                await app.RunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "The WebDaemon has crashed with a critical exception!");
            }
        }
    }
}
