using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PinEventAddOnCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "add_on_catalog_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_event_id_add_on_c_cf0612fdfefe",
                schema: "islamu_event",
                table: "registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" });

            migrationBuilder.AddForeignKey(
                name: "fk_registration_orders_event_add_on_catalog_versio_ba65b666098e",
                schema: "islamu_event",
                table: "registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" },
                principalSchema: "islamu_event",
                principalTable: "event_add_on_catalog_versions",
                principalColumns: new[] { "tenant_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_orders_event_add_on_catalog_versio_ba65b666098e",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropIndex(
                name: "ix_registration_orders_tenant_id_event_id_add_on_c_cf0612fdfefe",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "add_on_catalog_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders");
        }
    }
}
