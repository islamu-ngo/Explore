// ABOUTME: Verifies project-owned migration operation transformations are exact and collision-safe.
// ABOUTME: Covers governance lookup ordering, idempotence, and preserved monetary backfill SQL.

using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Event.Persistence.IntegrationTests.Migrations;

public sealed class ApplicationMigrationsModelDifferOperationTests
{
    [Test]
    public async Task CanonicalGovernanceOperationsInsertLookupRowsBeforeAddressColumns()
    {
        List<MigrationOperation> operations = GovernanceOperations();

        ApplicationMigrationsModelDiffer.ApplyProjectOwnedOperationTransformations(operations);

        InsertDataOperation[] inserts = operations.OfType<InsertDataOperation>().ToArray();
        await Assert.That(inserts.Length).IsEqualTo(2);
        await Assert.That(inserts.Count(operation => operation.Table == "location_address_sources")).IsEqualTo(1);
        await Assert.That(inserts.Count(operation => operation.Table == "location_address_visibilities")).IsEqualTo(1);

        int lastCreate = operations.FindLastIndex(operation => operation is CreateTableOperation);
        int firstInsert = operations.FindIndex(operation => operation is InsertDataOperation);
        int lastInsert = operations.FindLastIndex(operation => operation is InsertDataOperation);
        int firstAddressColumn = operations.FindIndex(IsAddressColumn);
        await Assert.That(lastCreate < firstInsert).IsTrue();
        await Assert.That(lastInsert < firstAddressColumn).IsTrue();
    }

    [Test]
    public async Task ApplyingTransformationsTwiceDoesNotDuplicateOrMoveOperations()
    {
        List<MigrationOperation> operations = GovernanceOperations();
        operations.AddRange(MonetaryOperations(includeSuffixCollisions: false));
        ApplicationMigrationsModelDiffer.ApplyProjectOwnedOperationTransformations(operations);
        MigrationOperation[] firstPass = [.. operations];

        ApplicationMigrationsModelDiffer.ApplyProjectOwnedOperationTransformations(operations);

        await Assert.That(operations.Count).IsEqualTo(firstPass.Length);
        for (int index = 0; index < firstPass.Length; index++)
        {
            await Assert.That(ReferenceEquals(operations[index], firstPass[index])).IsTrue();
        }
    }

    [Test]
    public async Task SuffixCollisionAndUnrelatedGovernanceTablesDoNotTriggerOrMoveOperations()
    {
        List<MigrationOperation> operations =
        [
            new CreateTableOperation { Name = "archived_location_address_sources" },
            new SqlOperation { Sql = "SELECT 1;" },
            new CreateTableOperation { Name = "other_location_address_visibilities" },
            AddressColumn("archived_locations", "address_source_id"),
            AddressColumn("unrelated_locations", "address_visibility_id"),
            AddressColumn("suffix_locations", "address_organization_id"),
        ];
        MigrationOperation[] before = [.. operations];

        ApplicationMigrationsModelDiffer.ApplyProjectOwnedOperationTransformations(operations);

        await Assert.That(operations.Count).IsEqualTo(before.Length);
        await Assert.That(operations.OfType<InsertDataOperation>().Count()).IsEqualTo(0);
        for (int index = 0; index < before.Length; index++)
        {
            await Assert.That(ReferenceEquals(operations[index], before[index])).IsTrue();
        }
    }

    [Test]
    public async Task CanonicalMonetaryColumnsPreserveExactBackfillOrderingAndIgnoreSuffixTables()
    {
        List<MigrationOperation> operations = MonetaryOperations(includeSuffixCollisions: true);

        ApplicationMigrationsModelDiffer.ApplyProjectOwnedOperationTransformations(operations);

        SqlOperation[] sqlOperations = operations.OfType<SqlOperation>().ToArray();
        await Assert.That(sqlOperations.Length).IsEqualTo(2);
        await Assert.That(sqlOperations[0].Sql).IsEqualTo(
            "UPDATE registration_orders SET pre_discount_organizer_directed_total_minor_snapshot = organizer_directed_total_minor_snapshot, post_discount_organizer_directed_total_minor_snapshot = organizer_directed_total_minor_snapshot;");
        await Assert.That(sqlOperations[1].Sql).IsEqualTo(
            "UPDATE ie_registration_order_lines SET pre_discount_line_subtotal_minor_snapshot = line_subtotal_snapshot, post_discount_line_subtotal_minor_snapshot = line_subtotal_snapshot;");

        int orderLastColumn = operations.FindLastIndex(operation =>
            operation is AddColumnOperation column
            && column.Table == "registration_orders"
            && column.Name == "post_discount_organizer_directed_total_minor_snapshot");
        int lineLastColumn = operations.FindLastIndex(operation =>
            operation is AddColumnOperation column
            && column.Table == "ie_registration_order_lines"
            && column.Name == "post_discount_line_subtotal_minor_snapshot");
        await Assert.That(ReferenceEquals(operations[orderLastColumn + 1], sqlOperations[0])).IsTrue();
        await Assert.That(ReferenceEquals(operations[lineLastColumn + 1], sqlOperations[1])).IsTrue();
        await Assert.That(sqlOperations.Any(operation => operation.Sql.Contains("archived_", StringComparison.Ordinal))).IsFalse();
    }

    private static List<MigrationOperation> GovernanceOperations() =>
    [
        new CreateTableOperation { Name = "location_address_sources", Schema = "islamu_event" },
        new CreateTableOperation { Name = "location_address_visibilities", Schema = "islamu_event" },
        new SqlOperation { Sql = "SELECT 1;" },
        AddressColumn("locations", "address_source_id"),
        AddressColumn("locations", "address_visibility_id"),
        AddressColumn("locations", "address_organization_id"),
    ];

    private static List<MigrationOperation> MonetaryOperations(bool includeSuffixCollisions)
    {
        List<MigrationOperation> operations =
        [
            MonetaryColumn("registration_orders", "pre_discount_organizer_directed_total_minor_snapshot"),
            MonetaryColumn("registration_orders", "post_discount_organizer_directed_total_minor_snapshot"),
            MonetaryColumn("ie_registration_order_lines", "pre_discount_line_subtotal_minor_snapshot"),
            MonetaryColumn("ie_registration_order_lines", "post_discount_line_subtotal_minor_snapshot"),
        ];
        if (includeSuffixCollisions)
        {
            operations.Insert(1, MonetaryColumn(
                "archived_registration_orders",
                "pre_discount_organizer_directed_total_minor_snapshot"));
            operations.Insert(3, MonetaryColumn(
                "archived_registration_orders",
                "post_discount_organizer_directed_total_minor_snapshot"));
            operations.Add(MonetaryColumn(
                "archived_registration_order_lines",
                "pre_discount_line_subtotal_minor_snapshot"));
            operations.Add(MonetaryColumn(
                "archived_registration_order_lines",
                "post_discount_line_subtotal_minor_snapshot"));
        }
        return operations;
    }

    private static AddColumnOperation AddressColumn(string table, string name) => new()
    {
        Table = table,
        Name = name,
        ClrType = name == "address_organization_id" ? typeof(Guid?) : typeof(int),
    };

    private static AddColumnOperation MonetaryColumn(string table, string name) => new()
    {
        Table = table,
        Name = name,
        ClrType = typeof(long),
    };

    private static bool IsAddressColumn(MigrationOperation operation) =>
        operation is AddColumnOperation
        {
            Table: "locations",
            Name: "address_source_id" or "address_visibility_id" or "address_organization_id",
        };
}
