// ABOUTME: Adds project-owned data operations that ordinary EF model differencing cannot infer.
// ABOUTME: Preserves monetary snapshots and seeds required address-governance FK principals during scaffolding.

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace Explore.Persistence.Schema;

internal sealed class ApplicationMigrationsModelDiffer(
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
        ApplyProjectOwnedOperationTransformations(operations);
        return operations;
    }

    internal static void ApplyProjectOwnedOperationTransformations(List<MigrationOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        InsertLocationAddressLookupRows(operations);
        InsertAfterAddedSnapshotColumns(operations);
    }

    private static void InsertLocationAddressLookupRows(List<MigrationOperation> operations)
    {
        const string sourceTable = "location_address_sources";
        const string visibilityTable = "location_address_visibilities";
        CreateTableOperation? sources = operations
            .OfType<CreateTableOperation>()
            .SingleOrDefault(operation => IsCanonicalTable(operation.Name, sourceTable));
        CreateTableOperation? visibilities = operations
            .OfType<CreateTableOperation>()
            .SingleOrDefault(operation => IsCanonicalTable(operation.Name, visibilityTable));
        if (sources is null
            || visibilities is null
            || operations.OfType<InsertDataOperation>().Any(operation =>
                IsCanonicalTable(operation.Table, sourceTable)
                || IsCanonicalTable(operation.Table, visibilityTable)))
        {
            return;
        }

        AddColumnOperation[] addressColumns = operations
            .OfType<AddColumnOperation>()
            .Where(operation => IsCanonicalTable(operation.Table, "locations")
                && operation.Name is "address_organization_id" or "address_source_id" or "address_visibility_id")
            .ToArray();
        foreach (AddColumnOperation addressColumn in addressColumns)
        {
            operations.Remove(addressColumn);
        }

        int insertAt = Math.Max(operations.IndexOf(sources), operations.IndexOf(visibilities)) + 1;
        operations.Insert(insertAt++, new InsertDataOperation
        {
            Table = sources.Name,
            Schema = sources.Schema,
            Columns = ["id", "master_code", "full_name", "description"],
            Values = new object[,]
            {
                { 1, "UNKNOWN_LEGACY", "Unknown legacy", "Address provenance predates explicit governance or is unknown" },
                { 2, "MANUAL", "Manual", "Address was entered locally without a provider selection" },
                { 3, "PROVIDER_SELECTION", "Provider selection", "Address originated from a protected provider selection" }
            }
        });
        operations.Insert(insertAt++, new InsertDataOperation
        {
            Table = visibilities.Name,
            Schema = visibilities.Schema,
            Columns = ["id", "master_code", "full_name", "description"],
            Values = new object[,]
            {
                { 1, "QUARANTINED", "Quarantined", "Address is unavailable for local suggestion reuse" },
                { 2, "CREATOR_PRIVATE", "Creator private", "Address reuse is limited to its creator" },
                { 3, "ORGANIZATION_SCOPED", "Organization scoped", "Address reuse is limited to one tenant organization participation" },
                { 4, "TENANT_APPROVED", "Tenant approved", "Address is approved for reuse across its tenant" }
            }
        });
        foreach (AddColumnOperation addressColumn in addressColumns)
        {
            operations.Insert(insertAt++, addressColumn);
        }
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
            .Where(operation => IsCanonicalTable(operation.Table, tableName))
            .ToArray();

        if (!snapshotColumns.All(column => addColumns.Any(operation => operation.Name == column)))
        {
            return;
        }

        var template = addColumns.First(operation => operation.Name == snapshotColumns[0]);
        string sql = BuildBackfillSql(template.Schema, template.Table, legacyColumn, snapshotColumns);
        if (operations.OfType<SqlOperation>().Any(operation =>
            string.Equals(operation.Sql, sql, StringComparison.Ordinal)))
        {
            return;
        }

        var insertAfter = operations.FindLastIndex(operation =>
            operation is AddColumnOperation addColumn
            && IsCanonicalTable(addColumn.Table, tableName)
            && snapshotColumns.Contains(addColumn.Name, StringComparer.Ordinal));
        operations.Insert(insertAfter + 1, new SqlOperation { Sql = sql });
    }

    private static bool IsCanonicalTable(string table, string logicalName) =>
        string.Equals(table, logicalName, StringComparison.Ordinal)
        || string.Equals(table, RelationalModelNamespace.Prefix + logicalName, StringComparison.Ordinal);

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
