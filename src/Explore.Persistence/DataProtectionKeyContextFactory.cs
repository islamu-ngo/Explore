// ABOUTME: Design-time factory for DataProtectionKeyContext migrations and schema updates.
// ABOUTME: Reuses the same discrete Postgres bootstrap flow as the primary ExploreDbContext factory.

using Explore.Secrets.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence;

public sealed class DataProtectionKeyContextFactory : IDesignTimeDbContextFactory<DataProtectionKeyContext>
{
    public DataProtectionKeyContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<DataProtectionKeyContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        BootstrapPostgresCredentials credentials;
        try
        {
            credentials = BootstrapSecretLoader.LoadPostgresConnectionString(configuration, logger: null);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                "Design-time DataProtectionKeyContext creation failed: no Postgres credentials could be resolved. Configure Infisical bootstrap user secrets or POSTGRESQL_* environment variables.",
                ex);
        }

        Console.WriteLine($"[DesignTime] Data Protection Postgres bootstrap source: {credentials.Source}");

        var optionsBuilder = new DbContextOptionsBuilder<DataProtectionKeyContext>();
        optionsBuilder
            .UseNpgsql(credentials.ConnectionString, b => b.MigrationsAssembly("Explore.Persistence"))
            .UseSnakeCaseNamingConvention();

        return new DataProtectionKeyContext(optionsBuilder.Options);
    }
}
