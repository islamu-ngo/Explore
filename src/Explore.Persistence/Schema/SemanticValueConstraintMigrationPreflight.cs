// ABOUTME: Rejects malformed legacy semantic values before non-transactional provider DDL begins.
// ABOUTME: Keeps MariaDB and MySQL semantic-constraint upgrades mutation-free and retryable after repair.

using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Schema;

internal static class SemanticValueConstraintMigrationPreflight
{
    private const string MigrationSuffix = "PersistSemanticValueConstraints";

    public static async Task ValidateAsync(
        ExploreDbContext database,
        CancellationToken cancellationToken)
    {
        string? providerName = database.Database.ProviderName;
        if (providerName is null
            || !providerName.Contains("MySql", StringComparison.Ordinal))
        {
            return;
        }

        string[] migrations = database.Database
            .GetMigrations()
            .ToArray();
        int semanticIndex = Array.FindIndex(
            migrations,
            migration => migration.EndsWith(
                MigrationSuffix,
                StringComparison.Ordinal));
        if (semanticIndex <= 0)
        {
            return;
        }

        HashSet<string> applied = (await database.Database
                .GetAppliedMigrationsAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        string semanticMigration = migrations[semanticIndex];
        string predecessor = migrations[semanticIndex - 1];
        if (applied.Contains(semanticMigration)
            || !applied.Contains(predecessor))
        {
            return;
        }

        string? violation = await FindViolationAsync(
            database,
            cancellationToken);
        if (violation is not null)
        {
            throw new InvalidOperationException(
                $"The semantic value migration cannot start because legacy {violation} data violates its required invariant. " +
                "Repair that data category and retry; no schema change was attempted.");
        }
    }

    private static async Task<string?> FindViolationAsync(
        ExploreDbContext database,
        CancellationToken cancellationToken)
    {
        DbConnection connection = database.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM ie_event_ticket_types
                        WHERE fixed_price_minor < 0
                           OR minimum_price_minor < 0
                           OR suggested_price_minor < 0)
                    THEN 'money'
                    WHEN EXISTS (
                        SELECT 1
                        FROM ie_location_pii
                        WHERE (latitude IS NULL AND longitude IS NOT NULL)
                           OR (latitude IS NOT NULL AND longitude IS NULL)
                           OR latitude < -90
                           OR latitude > 90
                           OR longitude < -180
                           OR longitude > 180)
                    THEN 'coordinate'
                    WHEN EXISTS (
                        SELECT 1
                        FROM ie_event_agenda_items
                        WHERE local_end_date < local_start_date)
                    THEN 'agenda date range'
                    WHEN EXISTS (
                        SELECT 1
                        FROM ie_event_sessions
                        WHERE local_end_date < local_start_date)
                    THEN 'session date range'
                    ELSE NULL
                END
                """;
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null or DBNull ? null : Convert.ToString(
                result,
                System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
