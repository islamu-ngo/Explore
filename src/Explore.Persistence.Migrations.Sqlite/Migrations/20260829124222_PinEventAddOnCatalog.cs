using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_event_id_add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" });

            migrationBuilder.AddForeignKey(
                name: "fk_registration_orders_event_add_on_catalog_versions_tenant_id_event_id_add_on_catalog_version_id_snapshot",
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
                name: "fk_registration_orders_event_add_on_catalog_versions_tenant_id_event_id_add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropIndex(
                name: "ix_registration_orders_tenant_id_event_id_add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders");
        }
    }
}
