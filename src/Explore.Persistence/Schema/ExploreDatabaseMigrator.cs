// ABOUTME: Provides the single application-facing entry point for Explore database migrations.
// ABOUTME: Applies the complete current migration set through the ordinary EF Core migration path.

using Microsoft.EntityFrameworkCore;
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

        return db.Database.MigrateAsync(cancellationToken);
    }
}
