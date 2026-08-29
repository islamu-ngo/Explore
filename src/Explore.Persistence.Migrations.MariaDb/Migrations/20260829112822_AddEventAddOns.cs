using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
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
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    retired_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_catalog_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit_price_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    inventory_capacity = table.Column<int>(type: "int", nullable: false),
                    fulfillment_disclosure = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_disclosure = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_catalog_items", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_catalog_items_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.UniqueConstraint("ak_ie_event_add_on_catalog_items_tenant_id_event_add_on_448129aa", x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.id });
                    table.CheckConstraint("ck_event_add_on_catalog_items_capacity", "inventory_capacity > 0");
                    table.CheckConstraint("ck_event_add_on_catalog_items_money", "unit_price_minor >= 0");
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_catalog_items_ie_event_add_on_catalo_05a3d12c",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_registration_order_add_on_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    name_snapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit_price_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    line_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    currency_code_snapshot = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fulfillment_disclosure_snapshot = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_disclosure_snapshot = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_order_add_on_lines", x => x.id);
                    table.UniqueConstraint("ak_ie_registration_order_add_on_lines_tenant_id_event_i_2a6a2928", x => new { x.tenant_id, x.event_id, x.registration_order_id, x.id });
                    table.UniqueConstraint("ak_registration_order_add_on_lines_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_order_add_on_lines_money", "unit_price_minor_snapshot >= 0 AND line_total_minor_snapshot >= 0");
                    table.CheckConstraint("ck_registration_order_add_on_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_ie_registration_order_add_on_lines_ie_event_add_on_c_cb43a004",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_registration_order_add_on_lines_ie_event_add_on_c_cec0f4ba",
                        columns: x => new { x.tenant_id, x.event_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_registration_order_add_on_lines_ie_registration_o_efb429c6",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_fulfillments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    fulfilled_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_fulfillments", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_fulfillments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_add_on_fulfillments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_fulfillments_ie_registration_order_a_69f823e2",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_inventory_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    released_quantity = table.Column<int>(type: "int", nullable: false),
                    active_uniqueness_slot = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reserved_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    released_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_inventory_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_inventory_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_inventory_allocations_quantity", "quantity > 0 AND released_quantity >= 0 AND released_quantity <= quantity");
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_inventory_allocations_ie_event_add_o_6fab57c5",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_inventory_allocations_ie_registratio_1f353da3",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_refund_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    refund_operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allocated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_refund_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_refund_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_refund_allocations_money", "amount_minor >= 0");
                    table.CheckConstraint("ck_event_add_on_refund_allocations_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_event_add_on_refund_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_refund_allocations_ie_registration_o_b166ff79",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_catalog_items_tenant_id_event_add_on_31d30e11",
                table: "ie_event_add_on_catalog_items",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_catalog_items_tenant_id_event_add_on_3c1a9f32",
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
                name: "ix_ie_event_add_on_catalog_versions_tenant_id_event_id__2953ee09",
                table: "ie_event_add_on_catalog_versions",
                columns: new[] { "tenant_id", "event_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_operation_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_fulfillments_tenant_id_event_id_regi_eb07ddbb",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_fulfillments_tenant_id_registration__3e46ee7a",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_operation_id",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_inventory_allocations_tenant_id_even_2d9802a1",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_add_on_catalog_item_id", "released_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_inventory_allocations_tenant_id_even_ee418489",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_inventory_allocations_tenant_id_regi_59a7a7f2",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id", "active_uniqueness_slot" },
                unique: true,
                filter: "active_uniqueness_slot IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_refund_allocations_tenant_id_event_i_5cc5cd44",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_refund_allocations_tenant_id_refund__60be1706",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "refund_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_refund_allocations_tenant_id_registr_8437c3f3",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_order_add_on_lines_tenant_id_event_a_f6f2a17b",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "event_add_on_catalog_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_order_add_on_lines_tenant_id_event_i_e2fd4950",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_id", "event_add_on_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_order_add_on_lines_tenant_id_registr_774eb183",
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
