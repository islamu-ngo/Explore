// ABOUTME: Result record for structured database connection-string composition.
// ABOUTME: Carries safe diagnostics alongside the derived process-local connection string.

namespace Explore.Secrets.Database;

public sealed record PrimaryDatabaseConnectionResult(
    PrimaryDatabaseRole Role,
    PrimaryDatabaseProvider Provider,
    string ConnectionString,
    string RedactedConnectionString,
    string SafeSummary);
