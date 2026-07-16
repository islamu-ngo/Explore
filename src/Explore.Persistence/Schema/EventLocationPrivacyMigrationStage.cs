// ABOUTME: Selects the exact operator-approved Event Location Privacy migration target.
// ABOUTME: Prevents EF from auto-advancing across Expand, Backfill, or Contract rollout gates.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Explore.Persistence.Schema;

public static class EventLocationPrivacyMigrationStage
{
    public const string ConfigurationKey = "Database:Migrations:EventLocationPrivacyStage";
    public const string Expand = "Expand";
    public const string Backfill = "Backfill";
    public const string Contract = "Contract";

    internal const string ExpandMigration = "20260716132239_AddEventLocationPrivacyExpand";
    internal const string BackfillMigration = "BackfillUnclassifiedEventLocations";
    internal const string ContractMigration = "ValidateAndContractEventLocationPrivacy";

    public static async Task MigrateAsync(
        ExploreDbContext db,
        string? configuredStage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        string[] migrations = db.Database.GetMigrations().ToArray();
        string[] applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        var appliedSet = applied.ToHashSet(StringComparer.Ordinal);
        var targets = ResolveTargets(migrations);
        var pendingStages = targets
            .Where(pair => pair.Value is not null && !appliedSet.Contains(pair.Value))
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);

        string? stage = Normalize(configuredStage);
        if (stage is null)
        {
            if (pendingStages.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{ConfigurationKey} is required while Event Location Privacy staged migrations are pending.");
            }

            await db.Database.MigrateAsync(cancellationToken);
            return;
        }

        if (!targets.TryGetValue(stage, out string? target) || target is null)
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey}={stage} does not map to an available migration target.");
        }

        EnsurePredecessorsApplied(stage, targets, appliedSet);

        if (pendingStages.Count > 0 && !pendingStages.Contains(stage))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKey}={stage} cannot skip or auto-advance the pending Event Location Privacy stage.");
        }

        if (appliedSet.Contains(target))
        {
            await db.Database.MigrateAsync(cancellationToken);
            return;
        }

        await db.GetService<IMigrator>().MigrateAsync(target, cancellationToken);
    }

    private static Dictionary<string, string?> ResolveTargets(IEnumerable<string> migrations)
    {
        string[] available = migrations.ToArray();
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [Expand] = ResolveExact(available, ExpandMigration),
            [Backfill] = ResolveByName(available, BackfillMigration),
            [Contract] = ResolveByName(available, ContractMigration)
        };
    }

    private static string? ResolveExact(IEnumerable<string> migrations, string target) =>
        migrations.SingleOrDefault(migration => string.Equals(migration, target, StringComparison.Ordinal));

    private static string? ResolveByName(IEnumerable<string> migrations, string targetName) =>
        migrations.SingleOrDefault(migration =>
            string.Equals(migration, targetName, StringComparison.Ordinal) ||
            migration.EndsWith($"_{targetName}", StringComparison.Ordinal));

    private static string? Normalize(string? configuredStage)
    {
        if (string.IsNullOrWhiteSpace(configuredStage))
        {
            return null;
        }

        string stage = configuredStage.Trim();
        return stage.ToUpperInvariant() switch
        {
            "EXPAND" => Expand,
            "BACKFILL" => Backfill,
            "CONTRACT" => Contract,
            _ => throw new InvalidOperationException(
                $"{ConfigurationKey} must be one of {Expand}, {Backfill}, or {Contract}.")
        };
    }

    private static void EnsurePredecessorsApplied(
        string stage,
        IReadOnlyDictionary<string, string?> targets,
        IReadOnlySet<string> applied)
    {
        if (stage is Backfill or Contract)
        {
            RequireApplied(Expand, targets, applied);
        }

        if (stage == Contract)
        {
            RequireApplied(Backfill, targets, applied);
        }
    }

    private static void RequireApplied(
        string predecessor,
        IReadOnlyDictionary<string, string?> targets,
        IReadOnlySet<string> applied)
    {
        if (!targets.TryGetValue(predecessor, out string? migration) ||
            migration is null ||
            !applied.Contains(migration))
        {
            throw new InvalidOperationException(
                $"Event Location Privacy stage {predecessor} must be applied before the requested stage.");
        }
    }
}
