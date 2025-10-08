using CaptivePortal.Daemons;
using CaptivePortal.Database;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Services.Outer
{
    public class OuterServiceProviderService
    {
        public IServiceProvider OuterServiceProvider { get; private set; }

        public OuterServiceProviderService(IServiceProvider outerSP)
        {
            OuterServiceProvider = outerSP;
        }

        public AppArgsService AppArgsService
            => OuterServiceProvider.GetRequiredService<AppArgsService>();

        public IronNacConfiguration IronNacConfiguration
            => OuterServiceProvider.GetRequiredService<IronNacConfiguration>();

        public RadiusDisconnectorService RadiusDisconnectorService
            => OuterServiceProvider.GetRequiredService<RadiusDisconnectorService>();

        public RadiusAttributeParserService RadiusAttributeParserService
            => OuterServiceProvider.GetRequiredService<RadiusAttributeParserService>();

        public WebAuthenticationService WebAuthenticationService
            => OuterServiceProvider.GetRequiredService<WebAuthenticationService>();

        public IDbContextFactory<IronNacDbContext> DbContextFactory
            => OuterServiceProvider.GetRequiredService<IDbContextFactory<IronNacDbContext>>();

        public DataRefreshNotificationService DataRefreshNotificationService
            => OuterServiceProvider.GetRequiredService<DataRefreshNotificationService>();

        public WebDaemon WebDaemon
            => OuterServiceProvider.GetRequiredService<WebDaemon>();

        public DnsDaemon DnsDaemon
            => OuterServiceProvider.GetRequiredService<DnsDaemon>();

        public RadiusAuthorizationDaemon RadiusAuthorizationDaemon
            => OuterServiceProvider.GetRequiredService<RadiusAuthorizationDaemon>();

        public RadiusAccountingDaemon RadiusAccountingDaemon
            => OuterServiceProvider.GetRequiredService<RadiusAccountingDaemon>();

        public static bool RegisterServicesInParent(IServiceCollection services, ILogger logger, string[] appArgs)
        {
            services.AddSingleton(new AppArgsService(appArgs));

            try
            {
                services.AddSingleton(new IronNacConfiguration());
            }
            catch (AggregateException ex)
            {
                logger.LogCritical(ex.Message);

                return false;
            }

            services.AddTransient<RadiusDisconnectorService>();

            services.AddSingleton<RadiusAttributeParserService>();

            services.AddScoped<WebAuthenticationService>();

            services.AddDbContextFactory<IronNacDbContext>();

            services.AddSingleton(new DataRefreshNotificationService());

            services.AddHostedService<WebDaemon>();

            services.AddHostedService<DnsDaemon>();

            services.AddHostedService<RadiusAuthorizationDaemon>();

            services.AddHostedService<RadiusAccountingDaemon>();

            return true;
        }

        public void RegisterServicesInChild(IServiceCollection services)
        {
            services.AddSingleton(this);

            services.AddSingleton(AppArgsService);

            services.AddSingleton(IronNacConfiguration);

            services.AddTransient(sp
                => sp.GetRequiredService<OuterServiceProviderService>().RadiusDisconnectorService);

            services.AddSingleton(RadiusAttributeParserService);

            services.AddScoped(sp
                => sp.GetRequiredService<OuterServiceProviderService>().WebAuthenticationService);

            services.AddSingleton(sp
                => sp.GetRequiredService<OuterServiceProviderService>().DbContextFactory);

            services.AddSingleton(DataRefreshNotificationService);

            // Accessing hosted services must be through the Outer Service Provider Service itself
            // otherwise a nesting loop of hosted services will be launched
            services.AddSingleton<DaemonInteractionService>();
        }
    }
}
