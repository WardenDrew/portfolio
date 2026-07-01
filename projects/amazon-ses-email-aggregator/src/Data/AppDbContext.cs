using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using SESAggregator.Configuration;

namespace SESAggregator.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(AssemblyMarker.Assembly);
    }

    public static IServiceCollection SetupServices(IServiceCollection services)
    {
        MySqlConnectionStringBuilder connectionStringBuilder = new();
        connectionStringBuilder.Server = DatabaseSettings.MYSQL_SERVER;
        connectionStringBuilder.Port = DatabaseSettings.MYSQL_PORT;
        connectionStringBuilder.Database = DatabaseSettings.MYSQL_DATABASE;
        connectionStringBuilder.UserID = DatabaseSettings.MYSQL_USERNAME;
        connectionStringBuilder.Password = DatabaseSettings.MYSQL_PASSWORD;
        string connectionString = connectionStringBuilder.ToString();

        ServerVersion version = ServerVersion.AutoDetect(connectionString);

        _ = services.AddDbContext<AppDbContext>(options =>
        {
            _ = options.UseMySql(connectionString, version)
                .ConfigureWarnings(x => x.Log((RelationalEventId.CommandExecuted, LogLevel.Debug)));
        });

        return services;
    }
}
