// ABOUTME: Verifies the lookup-relationship uniqueness migration stays focused and reversible.
// ABOUTME: Proves duplicate rows fail before the tenant indexes are replaced by composite unique indexes.

using Explore.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Event.Persistence.IntegrationTests.Migrations;

public sealed class LookupRelationshipUniquenessMigrationTests
{
    [Test]
    public async Task Up_FailsFastOnDuplicatesAndOnlyReplacesTheTwoIndexes()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        new TestableMigration().BuildUp(builder);

        await Assert.That(builder.Operations.Count).IsEqualTo(5);
        await Assert.That(builder.Operations[0] is SqlOperation).IsTrue();

        var preflight = (SqlOperation)builder.Operations[0];
        await Assert.That(preflight.Sql).Contains("duplicate (tenant_id, tag_id, tag_type_id) rows exist");
        await Assert.That(preflight.Sql).Contains("duplicate (tenant_id, category_id, category_type_id) rows exist");

        await Assert.That(builder.Operations.OfType<DropIndexOperation>().Select(operation => operation.Name))
            .IsEquivalentTo([
                "ix_tag_type_tags_tenant_id",
                "ix_category_type_categories_tenant_id"
            ]);
        await Assert.That(builder.Operations.OfType<CreateIndexOperation>().Select(DescribeIndex))
            .IsEquivalentTo([
                "ix_tag_type_tags_tenant_id_tag_id_tag_type_id:tenant_id,tag_id,tag_type_id:True",
                "ix_category_type_categories_tenant_id_category_id_category_typ:tenant_id,category_id,category_type_id:True"
            ]);
        await Assert.That(builder.Operations.OfType<CreateTableOperation>().Count()).IsEqualTo(0);
        await Assert.That(builder.Operations.OfType<DropColumnOperation>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Down_RestoresTheTwoTenantIndexes()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        new TestableMigration().BuildDown(builder);

        await Assert.That(builder.Operations.Count).IsEqualTo(4);
        await Assert.That(builder.Operations.OfType<DropIndexOperation>().Select(operation => operation.Name))
            .IsEquivalentTo([
                "ix_tag_type_tags_tenant_id_tag_id_tag_type_id",
                "ix_category_type_categories_tenant_id_category_id_category_typ"
            ]);
        await Assert.That(builder.Operations.OfType<CreateIndexOperation>().Select(DescribeIndex))
            .IsEquivalentTo([
                "ix_tag_type_tags_tenant_id:tenant_id:False",
                "ix_category_type_categories_tenant_id:tenant_id:False"
            ]);
    }

    private static string DescribeIndex(CreateIndexOperation operation) =>
        $"{operation.Name}:{string.Join(',', operation.Columns)}:{operation.IsUnique}";

    private sealed class TestableMigration : EnforceLookupRelationshipUniqueness
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);

        public void BuildDown(MigrationBuilder migrationBuilder) => Down(migrationBuilder);
    }
}
