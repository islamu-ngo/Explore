using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceLookupRelationshipUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM tag_type_tags
                        GROUP BY tenant_id, tag_id, tag_type_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot enforce tag_type_tags uniqueness: duplicate (tenant_id, tag_id, tag_type_id) rows exist.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM category_type_categories
                        GROUP BY tenant_id, category_id, category_type_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot enforce category_type_categories uniqueness: duplicate (tenant_id, category_id, category_type_id) rows exist.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropIndex(
                name: "ix_tag_type_tags_tenant_id",
                table: "tag_type_tags");

            migrationBuilder.DropIndex(
                name: "ix_category_type_categories_tenant_id",
                table: "category_type_categories");

            migrationBuilder.CreateIndex(
                name: "ix_tag_type_tags_tenant_id_tag_id_tag_type_id",
                table: "tag_type_tags",
                columns: new[] { "tenant_id", "tag_id", "tag_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_type_categories_tenant_id_category_id_category_typ",
                table: "category_type_categories",
                columns: new[] { "tenant_id", "category_id", "category_type_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tag_type_tags_tenant_id_tag_id_tag_type_id",
                table: "tag_type_tags");

            migrationBuilder.DropIndex(
                name: "ix_category_type_categories_tenant_id_category_id_category_typ",
                table: "category_type_categories");

            migrationBuilder.CreateIndex(
                name: "ix_tag_type_tags_tenant_id",
                table: "tag_type_tags",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_type_categories_tenant_id",
                table: "category_type_categories",
                column: "tenant_id");
        }
    }
}
