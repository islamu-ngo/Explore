using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAddressGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display_sort_key",
                schema: "islamu_event",
                table: "locations",
                type: "character varying(14000)",
                maxLength: 14000,
                nullable: false,
                defaultValue: "",
                collation: "C");

            migrationBuilder.AddColumn<short>(
                name: "display_sort_key_version",
                schema: "islamu_event",
                table: "locations",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "address_substring_key",
                schema: "islamu_event",
                table: "location_pii",
                type: "character varying(14000)",
                maxLength: 14000,
                nullable: false,
                defaultValue: "",
                collation: "C");

            migrationBuilder.AddColumn<short>(
                name: "address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "location_address_sources",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_address_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "location_address_visibilities",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_address_visibilities", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "islamu_event",
                table: "location_address_sources",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "UNKNOWN_LEGACY", "Unknown legacy", "Address provenance predates explicit governance or is unknown" },
                    { 2, "MANUAL", "Manual", "Address was entered locally without a provider selection" },
                    { 3, "PROVIDER_SELECTION", "Provider selection", "Address originated from a protected provider selection" }
                });

            migrationBuilder.InsertData(
                schema: "islamu_event",
                table: "location_address_visibilities",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "QUARANTINED", "Quarantined", "Address is unavailable for local suggestion reuse" },
                    { 2, "CREATOR_PRIVATE", "Creator private", "Address reuse is limited to its creator" },
                    { 3, "ORGANIZATION_SCOPED", "Organization scoped", "Address reuse is limited to one tenant organization participation" },
                    { 4, "TENANT_APPROVED", "Tenant approved", "Address is approved for reuse across its tenant" }
                });

            migrationBuilder.AddColumn<Guid>(
                name: "address_organization_id",
                schema: "islamu_event",
                table: "locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "address_source_id",
                schema: "islamu_event",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "address_visibility_id",
                schema: "islamu_event",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_locations_address_source_id",
                schema: "islamu_event",
                table: "locations",
                column: "address_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_address_visibility_id",
                schema: "islamu_event",
                table: "locations",
                column: "address_visibility_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_address_visibility_created_by",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_visibility_id", "created_by" });

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_address_visibility_organization",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_visibility_id", "address_organization_id" });

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_id_address_organization_id",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_organization_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_address_visibility_scope",
                schema: "islamu_event",
                table: "locations",
                sql: "(address_visibility_id = 1 AND address_organization_id IS NULL) OR (address_visibility_id = 2 AND created_by IS NOT NULL AND address_organization_id IS NULL) OR (address_visibility_id = 3 AND created_by IS NOT NULL AND address_organization_id IS NOT NULL) OR address_visibility_id = 4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_display_sort_key_version",
                schema: "islamu_event",
                table: "locations",
                sql: "(display_sort_key_version = 0 AND display_sort_key = '') OR (display_sort_key_version = 1 AND display_sort_key <> '' AND length(display_sort_key) % 7 = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_erased_address_quarantined",
                schema: "islamu_event",
                table: "locations",
                sql: "location_privacy_state_id <> 3 OR (address_visibility_id = 1 AND address_organization_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_private_home_address_visibility",
                schema: "islamu_event",
                table: "locations",
                sql: "location_kind_id <> 5 OR address_visibility_id <> 4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_tenant_approved_display_sort_key",
                schema: "islamu_event",
                table: "locations",
                sql: "address_visibility_id <> 4 OR display_sort_key_version = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_location_pii_address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii",
                sql: "(address_substring_key_version = 0 AND address_substring_key = '') OR (address_substring_key_version = 1 AND address_substring_key <> '' AND length(address_substring_key) % 7 = 0)");

            migrationBuilder.CreateIndex(
                name: "ix_location_address_sources_master_code",
                schema: "islamu_event",
                table: "location_address_sources",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_location_address_visibilities_master_code",
                schema: "islamu_event",
                table: "location_address_visibilities",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_location_address_sources_address_source_id",
                schema: "islamu_event",
                table: "locations",
                column: "address_source_id",
                principalSchema: "islamu_event",
                principalTable: "location_address_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_location_address_visibilities_address_visibility_",
                schema: "islamu_event",
                table: "locations",
                column: "address_visibility_id",
                principalSchema: "islamu_event",
                principalTable: "location_address_visibilities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_organization_tenants_tenant_id_address_organizati",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_organization_id" },
                principalSchema: "islamu_event",
                principalTable: "organization_tenants",
                principalColumns: new[] { "tenant_id", "organization_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_locations_location_address_sources_address_source_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_location_address_visibilities_address_visibility_",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_organization_tenants_tenant_id_address_organizati",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropTable(
                name: "location_address_sources",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "location_address_visibilities",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_locations_address_source_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_address_visibility_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_address_visibility_created_by",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_address_visibility_organization",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_id_address_organization_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_address_visibility_scope",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_display_sort_key_version",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_erased_address_quarantined",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_private_home_address_visibility",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_tenant_approved_display_sort_key",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_location_pii_address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii");

            migrationBuilder.DropColumn(
                name: "address_organization_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "address_source_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "address_visibility_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "display_sort_key",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "display_sort_key_version",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "address_substring_key",
                schema: "islamu_event",
                table: "location_pii");

            migrationBuilder.DropColumn(
                name: "address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii");
        }
    }
}
