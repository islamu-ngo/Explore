// ABOUTME: Enables SQLite write-ahead logging after the application schema is migrated.
// ABOUTME: Gates the SQLite-specific command so every other provider remains untouched.

using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Database;

public static class SqliteDatabaseInitializer
{
    public static Task InitializeAsync(DbContext database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        return database.Database.IsSqlite()
            ? database.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken)
            : Task.CompletedTask;
    }
}
