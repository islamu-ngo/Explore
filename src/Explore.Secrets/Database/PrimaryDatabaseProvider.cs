// ABOUTME: Closed primary database provider enum for structured composition.
// ABOUTME: Covers the supported primary engines without exposing raw connection strings.

namespace Explore.Secrets.Database;

public enum PrimaryDatabaseProvider
{
    PostgreSql = 1,
    Sqlite = 2,
    SqlServer = 3,
    MariaDb = 4,
    MySql = 5,
}
