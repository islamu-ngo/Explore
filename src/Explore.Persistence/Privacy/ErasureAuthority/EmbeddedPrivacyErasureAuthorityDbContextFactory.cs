// ABOUTME: Creates the embedded authority context from validated dedicated-file settings.
// ABOUTME: Keeps design-time migrations aligned with runtime SQLite composition.

using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence.Privacy.ErasureAuthority;

public sealed class EmbeddedPrivacyErasureAuthorityDbContextFactory
    : IDesignTimeDbContextFactory<EmbeddedPrivacyErasureAuthorityDbContext>
{
    public EmbeddedPrivacyErasureAuthorityDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<EmbeddedPrivacyErasureAuthorityDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        return CreateDbContext(configuration);
    }

    public EmbeddedPrivacyErasureAuthorityDbContext CreateDbContext(IConfiguration configuration)
    {
        EmbeddedPrivacyErasureAuthorityOptions embedded =
            EmbeddedPrivacyErasureAuthorityOptions.Bind(configuration);
        var options = new DbContextOptionsBuilder<EmbeddedPrivacyErasureAuthorityDbContext>();
        Configure(options, embedded);
        return new EmbeddedPrivacyErasureAuthorityDbContext(options.Options);
    }

    public static void Configure(
        DbContextOptionsBuilder options,
        EmbeddedPrivacyErasureAuthorityOptions embedded) =>
        options.UseSqlite(
                embedded.BuildConnectionString(),
                sqlite => sqlite
                    .MigrationsAssembly(EmbeddedPrivacyErasureAuthorityDbContext.MigrationsAssembly)
                    .MigrationsHistoryTable(EmbeddedPrivacyErasureAuthorityDbContext.MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new EmbeddedPrivacyErasureAuthorityConnectionInterceptor(
                embedded.BusyTimeoutSeconds));
}
