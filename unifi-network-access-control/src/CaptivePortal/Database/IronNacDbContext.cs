using CaptivePortal.Database.Entities;
using CaptivePortal.Services.Outer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data.Common;

namespace CaptivePortal.Database
{
    public class IronNacDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Network> Networks { get; set; }
        public DbSet<DeviceNetwork> DeviceNetworks { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<NetworkGroup> NetworkGroups { get; set; }
        public DbSet<UserNetworkGroup> UserNetworkGroups { get; set; }

        private readonly IronNacConfiguration configuration;
        private readonly ILogger logger;

        public IronNacDbContext(
            IronNacConfiguration configuration, 
            ILogger<IronNacDbContext> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            NpgsqlConnectionStringBuilder csb = new();
            csb.Host = configuration.DatabaseHostname;
            csb.Port = configuration.DatabasePort;
            csb.Database = configuration.DatabaseDbName;
            csb.Username = configuration.DatabaseUsername;
            csb.Password = configuration.DatabasePassword;
            csb.Pooling = true;
            csb.MaxPoolSize = 50;

            optionsBuilder.UseNpgsql(csb.ToString());
        }

        public async Task SeedDatabase(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Processing Database Seeding Data");

            string initialPassword = "password";
            User firstUser = new()
            {
                Name = "Default Administrator",
                Email = "admin@localhost",
                Hash = WebAuthenticationService.GetHash(initialPassword),
                ChangePasswordNextLogin = true,
                PermissionLevel = CaptivePortal.Models.PermissionLevel.Admin
            };
            this.Users.Add(firstUser);
            logger.LogInformation("Seeded Default Administrator {email} with {password}", firstUser.Email, initialPassword);

            NetworkGroup registrationNetworkGroup = new()
            {
                Registration = true,
                IsPool = true,
                Name = "Registration Networks"
            };
            this.NetworkGroups.Add(registrationNetworkGroup);
            logger.LogInformation("Seeded Registration Network Group");

            NetworkGroup guestNetworkGroup = new()
            {
                Guest = true,
                IsPool = true,
                Name = "Student / Guest Network Pool"
            };
            this.NetworkGroups.Add(guestNetworkGroup);
            logger.LogInformation("Seeded Guest Network Group");

            await this.SaveChangesAsync(cancellationToken);
        }
    }
}
