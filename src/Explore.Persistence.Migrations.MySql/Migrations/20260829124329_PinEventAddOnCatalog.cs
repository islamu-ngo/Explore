using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class PinEventAddOnCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_orders_tenant_id_event_id_add_on_cat_6a2d9031",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" });

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_orders_ie_event_add_on_catalog_versi_58105e19",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" },
                principalTable: "ie_event_add_on_catalog_versions",
                principalColumns: new[] { "tenant_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_orders_ie_event_add_on_catalog_versi_58105e19",
                table: "ie_registration_orders");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_orders_tenant_id_event_id_add_on_cat_6a2d9031",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders");
        }
    }
}
