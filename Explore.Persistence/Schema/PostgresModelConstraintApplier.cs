// ABOUTME: Applies PostgreSQL constraints declared as EF model metadata after standard migrations run.
// ABOUTME: Bridges Npgsql gaps such as exclusion constraints while keeping entity configuration authoritative.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Explore.Persistence.Schema;

public static class PostgresModelConstraintApplier
{
    private const string BtreeGistExtensionName = "btree_gist";

    public static async Task ApplyAsync(ExploreDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var constraints = GetConfiguredConstraints(context.Model);
        if (constraints.Count == 0)
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync(
            $"CREATE EXTENSION IF NOT EXISTS {BtreeGistExtensionName};",
            cancellationToken);

        foreach (var constraint in constraints)
        {
            await context.Database.ExecuteSqlRawAsync(BuildAddConstraintSql(constraint), cancellationToken);
        }
    }

    private static List<ModelExclusionConstraint> GetConfiguredConstraints(IModel model)
    {
        return model.GetEntityTypes()
            .SelectMany(GetEntityConstraints)
            .OrderBy(c => c.TableName, StringComparer.Ordinal)
            .ThenBy(c => c.Descriptor.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<ModelExclusionConstraint> GetEntityConstraints(IEntityType entityType)
    {
        var tableName = entityType.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            yield break;
        }

        foreach (var annotation in entityType.GetAnnotations()
                     .Where(a => PostgresExclusionConstraintExtensions.IsExclusionConstraintAnnotation(a.Name)))
        {
            yield return new ModelExclusionConstraint(
                tableName,
                entityType.GetSchema(),
                PostgresExclusionConstraintExtensions.ParseDescriptor(annotation.Value));
        }
    }

    private static string BuildAddConstraintSql(ModelExclusionConstraint constraint)
    {
        var tableIdentifier = QuoteQualifiedIdentifier(constraint.Schema, constraint.TableName);
        var tableRegClass = QuoteLiteral(
            string.IsNullOrWhiteSpace(constraint.Schema)
                ? constraint.TableName
                : $"{constraint.Schema}.{constraint.TableName}");
        var constraintName = QuoteIdentifier(constraint.Descriptor.Name);
        var constraintNameLiteral = QuoteLiteral(constraint.Descriptor.Name);
        var preflightBlock = BuildPreflightBlock(constraint.Descriptor);

        return $$"""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = {{constraintNameLiteral}}
                      AND conrelid = {{tableRegClass}}::regclass
                ) THEN
                    {{preflightBlock}}
                    ALTER TABLE {{tableIdentifier}}
                    ADD CONSTRAINT {{constraintName}}
                    EXCLUDE USING {{constraint.Descriptor.UsingMethod}} (
                        {{constraint.Descriptor.ElementsSql}}
                    )
                    WHERE ({{constraint.Descriptor.PredicateSql}});
                END IF;
            END $$;
            """;
    }

    private static string BuildPreflightBlock(PostgresExclusionConstraintDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.PreflightConflictExistsSql))
        {
            return string.Empty;
        }

        var failureMessage = QuoteLiteral(
            string.IsNullOrWhiteSpace(descriptor.PreflightFailureMessage)
                ? $"Cannot add PostgreSQL exclusion constraint {descriptor.Name} because existing rows violate it."
                : descriptor.PreflightFailureMessage);

        return $$"""
            IF EXISTS (
                {{descriptor.PreflightConflictExistsSql}}
            ) THEN
                RAISE EXCEPTION {{failureMessage}};
            END IF;

        """;
    }

    private static string QuoteQualifiedIdentifier(string? schema, string tableName)
    {
        return string.IsNullOrWhiteSpace(schema)
            ? QuoteIdentifier(tableName)
            : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)}";
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string QuoteLiteral(string value)
    {
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private sealed record ModelExclusionConstraint(
        string TableName,
        string? Schema,
        PostgresExclusionConstraintDescriptor Descriptor);
}
