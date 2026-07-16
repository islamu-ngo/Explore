// ABOUTME: Provides the single application-facing entry point for Explore database migrations.
// ABOUTME: Keeps feature-specific staged rollout policies inside Persistence instead of migration hosts.

using Microsoft.Extensions.Configuration;

namespace Explore.Persistence.Schema;

public static class ExploreDatabaseMigrator
{
    public static Task MigrateAsync(
        ExploreDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(configuration);

        return EventLocationPrivacyMigrationStage.MigrateAsync(
            db,
            configuration[EventLocationPrivacyMigrationStage.ConfigurationKey],
            cancellationToken);
    }
}
