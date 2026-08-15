// ABOUTME: Adds generated backfill SQL for promotion-era monetary snapshot columns.
// ABOUTME: Keeps historical registration orders and lines value-preserving when EF scaffolds migrations.

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace Explore.Persistence.Schema;

#pragma warning disable EF1001
internal sealed class PromotionSnapshotBackfillMigrationsModelDiffer(
    IRelationalTypeMappingSource typeMappingSource,
    IMigrationsAnnotationProvider migrationsAnnotationProvider,
    IRelationalAnnotationProvider relationalAnnotationProvider,
    IRowIdentityMapFactory rowIdentityMapFactory,
    CommandBatchPreparerDependencies commandBatchPreparerDependencies)
    : MigrationsModelDiffer(
        typeMappingSource,
        migrationsAnnotationProvider,
        relationalAnnotationProvider,
        rowIdentityMapFactory,
        commandBatchPreparerDependencies)
{
    public override IReadOnlyList<MigrationOperation> GetDifferences(
        IRelationalModel? source,
        IRelationalModel? target)
    {
        var operations = base.GetDifferences(source, target).ToList();
        InsertAfterAddedSnapshotColumns(operations);
        return operations;
    }

    private static void InsertAfterAddedSnapshotColumns(List<MigrationOperation> operations)
    {
        InsertBackfill(
            operations,
            tableName: "registration_orders",
            legacyColumn: "organizer_directed_total_minor_snapshot",
            snapshotColumns:
            [
                "pre_discount_organizer_directed_total_minor_snapshot",
                "post_discount_organizer_directed_total_minor_snapshot"
            ]);
        InsertBackfill(
            operations,
            tableName: "registration_order_lines",
            legacyColumn: "line_subtotal_snapshot",
            snapshotColumns:
            [
                "pre_discount_line_subtotal_minor_snapshot",
                "post_discount_line_subtotal_minor_snapshot"
            ]);
    }

    private static void InsertBackfill(
        List<MigrationOperation> operations,
        string tableName,
        string legacyColumn,
        string[] snapshotColumns)
    {
        var addColumns = operations
            .OfType<AddColumnOperation>()
            .Where(operation => operation.Table.EndsWith(tableName, StringComparison.Ordinal))
            .ToArray();

        if (!snapshotColumns.All(column => addColumns.Any(operation => operation.Name == column)))
        {
            return;
        }

        var insertAfter = operations.FindLastIndex(operation =>
            operation is AddColumnOperation addColumn &&
            addColumn.Table.EndsWith(tableName, StringComparison.Ordinal) &&
            snapshotColumns.Contains(addColumn.Name, StringComparer.Ordinal));
        var template = addColumns.First(operation => operation.Name == snapshotColumns[0]);
        operations.Insert(insertAfter + 1, new SqlOperation
        {
            Sql = BuildBackfillSql(template.Schema, template.Table, legacyColumn, snapshotColumns)
        });
    }

    private static string BuildBackfillSql(
        string? schema,
        string table,
        string legacyColumn,
        IReadOnlyList<string> snapshotColumns)
    {
        var qualifiedTable = schema is null ? table : $"{schema}.{table}";
        var assignments = string.Join(", ", snapshotColumns.Select(column => $"{column} = {legacyColumn}"));
        return $"UPDATE {qualifiedTable} SET {assignments};";
    }
}
#pragma warning restore EF1001
