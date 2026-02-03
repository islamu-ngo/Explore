using Explore.Secrets.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence
{
    /// <summary>
    /// Design-time factory for ExploreDbContext used by EF Core migrations.
    /// Fetches connection string from Infisical using bootstrap credentials from user secrets.
    /// </summary>
    /// <remarks>
    /// This factory uses Infisical to securely fetch the database connection string.
    /// User secrets only contain Infisical bootstrap credentials, NOT the database password.
    /// 
    /// Required user secrets (Infisical bootstrap credentials):
    ///   dotnet user-secrets set "Infisical:ProjectId" "your-project-id" --project Explore.Persistence
    ///   dotnet user-secrets set "Infisical:ClientId" "your-client-id" --project Explore.Persistence
    ///   dotnet user-secrets set "Infisical:ClientSecret" "your-client-secret" --project Explore.Persistence
    ///   dotnet user-secrets set "Infisical:Environment" "dev" --project Explore.Persistence
    /// 
    /// Then run migrations:
    ///   dotnet ef migrations add MigrationName --project Explore.Persistence --startup-project Explore.API
    /// </remarks>
    public class ExploreDbContextFactory : IDesignTimeDbContextFactory<ExploreDbContext>
    {
        public ExploreDbContext CreateDbContext(string[] args)
        {
            // Step 1: Build bootstrap configuration from user secrets (contains Infisical credentials)
            var bootstrapConfig = new ConfigurationBuilder()
                .AddUserSecrets<ExploreDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Step 2: Add Infisical to fetch actual secrets (including database connection string)
            var configBuilder = new ConfigurationBuilder()
                .AddUserSecrets<ExploreDbContextFactory>(optional: true)
                .AddEnvironmentVariables();

            // Add Infisical as configuration source to fetch secrets
            configBuilder.AddInfisical(bootstrapConfig, source =>
            {
                source.Paths.Clear();
                source.Paths.Add("/postgresql");
                source.ThrowOnFirstLoadFailure = false;
            });

            var configuration = configBuilder.Build();

            // Step 3: Get connection string (from Infisical or fallback to env var)
            var connectionString =
                configuration["POSTGRESQL_PUBLIC_URL"]
                ?? configuration["ConnectionStrings:DefaultConnection"]
                ?? Environment.GetEnvironmentVariable("POSTGRESQL_PUBLIC_URL")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    """
                    Connection string not found for design-time DbContext creation.

                    The factory tried to fetch it from Infisical but failed.

                    Please ensure your Infisical bootstrap credentials are set in user secrets:

                      dotnet user-secrets set "Infisical:ProjectId" "your-project-id" --project Explore.Persistence
                      dotnet user-secrets set "Infisical:ClientId" "your-client-id" --project Explore.Persistence
                      dotnet user-secrets set "Infisical:ClientSecret" "your-client-secret" --project Explore.Persistence
                      dotnet user-secrets set "Infisical:Environment" "dev" --project Explore.Persistence

                    Or set the connection string directly via environment variable:
                      $env:POSTGRESQL_PUBLIC_URL = "Host=...;Database=...;Username=...;Password=..."

                    Then run your migration command again.
                    """);
            }

            Console.WriteLine("[DesignTime] Connection string loaded successfully from Infisical");

            var optionsBuilder = new DbContextOptionsBuilder<ExploreDbContext>();
            optionsBuilder
                .UseNpgsql(connectionString, b => b.MigrationsAssembly("Explore.Persistence"))
                .UseSnakeCaseNamingConvention();

            return new ExploreDbContext(optionsBuilder.Options);
        }
    }
}
