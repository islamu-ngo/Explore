using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAddOns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "add_on_total_minor_snapshot",
                table: "ie_registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_catalog_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    version_number = table.Column<int>(type: "INTEGER", nullable: false),
                    published_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    retired_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_catalog_versions", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_catalog_versions_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_add_on_catalog_versions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_catalog_versions_lifecycle", "retired_at IS NULL OR (published_at IS NOT NULL AND retired_at >= published_at)");
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_versions_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_catalog_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    unit_price_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    inventory_capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    fulfillment_disclosure = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    refund_disclosure = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_catalog_items", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_id", x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.id });
                    table.UniqueConstraint("ak_event_add_on_catalog_items_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_catalog_items_capacity", "inventory_capacity > 0");
                    table.CheckConstraint("ck_event_add_on_catalog_items_money", "unit_price_minor >= 0");
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_items_event_add_on_catalog_versions_tenant_id_event_add_on_catalog_version_id",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_registration_order_add_on_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    name_snapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    unit_price_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    line_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    currency_code_snapshot = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    fulfillment_disclosure_snapshot = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    refund_disclosure_snapshot = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_order_add_on_lines", x => x.id);
                    table.UniqueConstraint("ak_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_id", x => new { x.tenant_id, x.event_id, x.registration_order_id, x.id });
                    table.UniqueConstraint("ak_registration_order_add_on_lines_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_order_add_on_lines_money", "unit_price_minor_snapshot >= 0 AND line_total_minor_snapshot >= 0");
                    table.CheckConstraint("ck_registration_order_add_on_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_event_add_o_ae0eaf0b7f25",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_event_add_on_catalog_versions_tenant_id_event_id_event_add_on_catalog_version_id",
                        columns: x => new { x.tenant_id, x.event_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_registration_orders_tenant_id_event_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_fulfillments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    fulfilled_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_fulfillments", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_fulfillments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_add_on_fulfillments_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_registration__2813f39d06a6",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_fulfillments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_inventory_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    released_quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    active_uniqueness_slot = table.Column<Guid>(type: "TEXT", nullable: true),
                    reserved_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    released_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_inventory_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_inventory_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_inventory_allocations_quantity", "quantity > 0 AND released_quantity >= 0 AND released_quantity <= quantity");
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_event_add_on_catalog_items_tenant_id_event_add_on_catalog_item_id",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_regi_5156e1858f3f",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_refund_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    refund_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    allocated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_refund_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_refund_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_refund_allocations_money", "amount_minor >= 0");
                    table.CheckConstraint("ck_event_add_on_refund_allocations_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_event_add_on_refund_allocations_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_registr_d4eabd70ae6e",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_refund_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_id",
                table: "ie_event_add_on_catalog_items",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_name",
                table: "ie_event_add_on_catalog_items",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_versions_tenant_id_event_id",
                table: "ie_event_add_on_catalog_versions",
                columns: new[] { "tenant_id", "event_id" },
                unique: true,
                filter: "published_at IS NOT NULL AND retired_at IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_versions_tenant_id_event_id_version_number",
                table: "ie_event_add_on_catalog_versions",
                columns: new[] { "tenant_id", "event_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_event_id_registration_order_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_operation_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_event_add_on_catalog_item_id_released_at",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_add_on_catalog_item_id", "released_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_event_id_registration_order_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_operation_id",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_registration_order_add_on_line_id_active_uniqueness_slot",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id", "active_uniqueness_slot" },
                unique: true,
                filter: "active_uniqueness_slot IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_refund_allocations_tenant_id_event_id_registration_order_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_refund_allocations_tenant_id_refund_operation_id",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "refund_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_refund_allocations_tenant_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_add_on_lines_tenant_id_event_add_on_catalog_version_id_event_add_on_catalog_item_id",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "event_add_on_catalog_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_add_on_lines_tenant_id_event_id_event_add_on_catalog_version_id",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_id", "event_add_on_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_add_on_lines_tenant_id_registration_order_id_event_add_on_catalog_item_id",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "registration_order_id", "event_add_on_catalog_item_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_event_add_on_fulfillments");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_inventory_allocations");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_refund_allocations");

            migrationBuilder.DropTable(
                name: "ie_registration_order_add_on_lines");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_catalog_items");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_catalog_versions");

            migrationBuilder.DropColumn(
                name: "add_on_total_minor_snapshot",
                table: "ie_registration_orders");
        }
    }
}
