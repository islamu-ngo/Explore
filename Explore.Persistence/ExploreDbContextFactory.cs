// ABOUTME: Design-time factory for EF Core migrations/scaffolding.
// ABOUTME: Routes all Postgres credentials through BootstrapSecretLoader - no URL form, no dual-source fallback.

using Explore.Secrets.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence;

/// <summary>
/// Design-time factory for <see cref="ExploreDbContext"/> used by the EF Core tooling
/// (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>, etc.).
/// </summary>
/// <remarks>
/// Resolution order (identical to runtime, see <see cref="BootstrapSecretLoader"/>):
/// Infisical (folder <c>/postgresql</c>) -> process environment -> appsettings-shaped <c>Postgresql:*</c> config.
/// No URL form. Each field (Host, Port, Database, Username, Password) resolves independently.
///
/// To generate migrations locally against an Infisical-backed project, set the SDK bootstrap creds
/// as user secrets on this project (bare Infisical:* keys are the canonical convention):
/// <code>
///   dotnet user-secrets set "Infisical:Url"          "https://app.infisical.com" --project Explore.Persistence
///   dotnet user-secrets set "Infisical:ProjectId"    "&lt;project-id&gt;"              --project Explore.Persistence
///   dotnet user-secrets set "Infisical:ClientId"     "&lt;client-id&gt;"               --project Explore.Persistence
///   dotnet user-secrets set "Infisical:ClientSecret" "&lt;secret&gt;"                  --project Explore.Persistence
///   dotnet user-secrets set "Infisical:Environment"  "dev"                       --project Explore.Persistence
/// </code>
/// Or, without Infisical, provide the discrete Postgres env vars:
/// <code>
///   POSTGRESQL_HOST, POSTGRESQL_PORT, POSTGRESQL_DATABASE, POSTGRESQL_USERNAME, POSTGRESQL_PASSWORD
/// </code>
/// </remarks>
public class ExploreDbContextFactory : IDesignTimeDbContextFactory<ExploreDbContext>
{
    public ExploreDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ExploreDbContextFactory>(optional: true)
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
                """
                Design-time DbContext creation failed: no Postgres credentials could be resolved.

                Provide ONE of the following:

                1. Infisical bootstrap user secrets (preferred for shared teams):
                     dotnet user-secrets set "Infisical:Url"          "https://app.infisical.com" --project Explore.Persistence
                     dotnet user-secrets set "Infisical:ProjectId"    "<project-id>"              --project Explore.Persistence
                     dotnet user-secrets set "Infisical:ClientId"     "<client-id>"               --project Explore.Persistence
                     dotnet user-secrets set "Infisical:ClientSecret" "<secret>"                  --project Explore.Persistence
                     dotnet user-secrets set "Infisical:Environment"  "dev"                       --project Explore.Persistence
                   (Folder /postgresql must contain POSTGRESQL_HOST, POSTGRESQL_PORT, POSTGRESQL_DATABASE, POSTGRESQL_USERNAME, POSTGRESQL_PASSWORD.)
                   (The legacy "SecretProvider:Infisical:*" prefix is still accepted as a fallback.)

                2. Discrete environment variables:
                     POSTGRESQL_HOST, POSTGRESQL_PORT, POSTGRESQL_DATABASE, POSTGRESQL_USERNAME, POSTGRESQL_PASSWORD

                3. Appsettings-shaped user secrets / env:
                     Postgresql:Host, Postgresql:Port, Postgresql:Database, Postgresql:Username, Postgresql:Password

                The URL form (POSTGRESQL_PUBLIC_URL) is no longer supported.
                """,
                ex);
        }

        Console.WriteLine($"[DesignTime] Postgres bootstrap source: {credentials.Source}");

        var optionsBuilder = new DbContextOptionsBuilder<ExploreDbContext>();
        optionsBuilder
            .UseNpgsql(credentials.ConnectionString, b => b.MigrationsAssembly("Explore.Persistence"))
            .UseSnakeCaseNamingConvention();

        return new ExploreDbContext(optionsBuilder.Options);
    }
}
