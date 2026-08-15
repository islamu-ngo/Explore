using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPromotionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "active_promotion_reservation_id",
                table: "ie_registration_orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "applied_promotion_code_id_snapshot",
                table: "ie_registration_orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "applied_promotion_definition_version_id_snapshot",
                table: "ie_registration_orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "applied_promotion_display_label_snapshot",
                table: "ie_registration_orders",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "post_discount_organizer_directed_total_minor_snapshot",
                table: "ie_registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "pre_discount_organizer_directed_total_minor_snapshot",
                table: "ie_registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE ie_registration_orders SET pre_discount_organizer_directed_total_minor_snapshot = organizer_directed_total_minor_snapshot, post_discount_organizer_directed_total_minor_snapshot = organizer_directed_total_minor_snapshot;");

            migrationBuilder.AddColumn<long>(
                name: "promotion_discount_total_minor_snapshot",
                table: "ie_registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "is_email_verified",
                table: "ie_registration_order_pii",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "post_discount_line_subtotal_minor_snapshot",
                table: "ie_registration_order_lines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "pre_discount_line_subtotal_minor_snapshot",
                table: "ie_registration_order_lines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE ie_registration_order_lines SET pre_discount_line_subtotal_minor_snapshot = line_subtotal_snapshot, post_discount_line_subtotal_minor_snapshot = line_subtotal_snapshot;");

            migrationBuilder.AddColumn<long>(
                name: "promotion_discount_amount_minor_snapshot",
                table: "ie_registration_order_lines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ie_promotion_definition_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    master_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_definition_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_promotion_reservation_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false),
                    master_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_reservation_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_promotion_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    definition_group_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version_number = table.Column<int>(type: "INTEGER", nullable: false),
                    promotion_definition_status_id = table.Column<int>(type: "INTEGER", nullable: false),
                    scope_metadata = table.Column<string>(type: "TEXT", nullable: false),
                    display_label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    eligibility = table.Column<string>(type: "TEXT", nullable: false),
                    discount_rule = table.Column<string>(type: "TEXT", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ends_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    total_redemption_limit = table.Column<int>(type: "INTEGER", nullable: true),
                    per_verified_purchaser_limit = table.Column<int>(type: "INTEGER", nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    scope_currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    scope_event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scope_ticket_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scope_ticket_catalog_version_number = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_definitions", x => x.id);
                    table.UniqueConstraint("ak_promotion_definitions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_ie_promotion_definitions_promotion_definition_statuses_promotion_definition_status_id",
                        column: x => x.promotion_definition_status_id,
                        principalTable: "ie_promotion_definition_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_promotion_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_promotion_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    promotion_definition_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scope_metadata = table.Column<string>(type: "TEXT", nullable: false),
                    display_label = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    lookup_digest = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    lookup_key_version = table.Column<int>(type: "INTEGER", nullable: false),
                    retired_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    scope_currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    scope_event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scope_ticket_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scope_ticket_catalog_version_number = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_codes", x => x.id);
                    table.UniqueConstraint("ak_promotion_codes_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_ie_promotion_codes_promotion_definitions_tenant_id_promotion_definition_version_id",
                        columns: x => new { x.tenant_id, x.promotion_definition_version_id },
                        principalTable: "ie_promotion_definitions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_promotion_codes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_promotion_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    promotion_definition_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    promotion_code_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    promotion_reservation_status_id = table.Column<int>(type: "INTEGER", nullable: false),
                    order_reservation_slot = table.Column<Guid>(type: "TEXT", nullable: false),
                    reserved_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    released_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    expired_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_reservations", x => x.id);
                    table.UniqueConstraint("ak_promotion_reservations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_promotion_reservation_active_slot", "(promotion_reservation_status_id = 1 AND order_reservation_slot = '00000000-0000-0000-0000-000000000000') OR (promotion_reservation_status_id <> 1 AND order_reservation_slot = id)");
                    table.CheckConstraint("ck_promotion_reservation_status_timestamps", "(promotion_reservation_status_id = 1 AND consumed_at_utc IS NULL AND released_at_utc IS NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 2 AND consumed_at_utc IS NOT NULL AND released_at_utc IS NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 3 AND consumed_at_utc IS NULL AND released_at_utc IS NOT NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 4 AND consumed_at_utc IS NULL AND released_at_utc IS NULL AND expired_at_utc IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_ie_promotion_reservations_ie_promotion_codes_tenant_id_promotion_code_id",
                        columns: x => new { x.tenant_id, x.promotion_code_id },
                        principalTable: "ie_promotion_codes",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_promotion_reservations_ie_promotion_definitions_tenant_id_promotion_definition_version_id",
                        columns: x => new { x.tenant_id, x.promotion_definition_version_id },
                        principalTable: "ie_promotion_definitions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_promotion_reservations_promotion_reservation_statuses_promotion_reservation_status_id",
                        column: x => x.promotion_reservation_status_id,
                        principalTable: "ie_promotion_reservation_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_promotion_reservations_registration_orders_tenant_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_promotion_reservations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_orders_tenant_id_applied_promotion_code_id_snapshot",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_code_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_orders_tenant_id_applied_promotion_definition_version_id_snapshot",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_definition_version_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_codes_tenant_id_promotion_definition_version_id_is_active",
                table: "ie_promotion_codes",
                columns: new[] { "tenant_id", "promotion_definition_version_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_codes_tenant_id_scope_event_id_scope_ticket_catalog_version_id_lookup_key_version",
                table: "ie_promotion_codes",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "lookup_key_version" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_codes_tenant_id_scope_event_id_scope_ticket_catalog_version_id_lookup_key_version_lookup_digest",
                table: "ie_promotion_codes",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_definition_statuses_master_code",
                table: "ie_promotion_definition_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_definitions_promotion_definition_status_id",
                table: "ie_promotion_definitions",
                column: "promotion_definition_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_definitions_tenant_id_definition_group_id_version_number",
                table: "ie_promotion_definitions",
                columns: new[] { "tenant_id", "definition_group_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_definitions_tenant_id_scope_event_id_scope_ticket_catalog_version_id_promotion_definition_status_id",
                table: "ie_promotion_definitions",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "promotion_definition_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservation_statuses_master_code",
                table: "ie_promotion_reservation_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservations_promotion_reservation_status_id",
                table: "ie_promotion_reservations",
                column: "promotion_reservation_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservations_registration_order_id_order_reservation_slot",
                table: "ie_promotion_reservations",
                columns: new[] { "registration_order_id", "order_reservation_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservations_tenant_id_promotion_code_id",
                table: "ie_promotion_reservations",
                columns: new[] { "tenant_id", "promotion_code_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservations_tenant_id_promotion_definition_version_id_promotion_reservation_status_id",
                table: "ie_promotion_reservations",
                columns: new[] { "tenant_id", "promotion_definition_version_id", "promotion_reservation_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservations_tenant_id_registration_order_id",
                table: "ie_promotion_reservations",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_orders_ie_promotion_codes_tenant_id_applied_promotion_code_id_snapshot",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_code_id_snapshot" },
                principalTable: "ie_promotion_codes",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_orders_ie_promotion_definitions_tenant_id_applied_promotion_definition_version_id_snapshot",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_definition_version_id_snapshot" },
                principalTable: "ie_promotion_definitions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_orders_ie_promotion_codes_tenant_id_applied_promotion_code_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_ie_registration_orders_ie_promotion_definitions_tenant_id_applied_promotion_definition_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropTable(
                name: "ie_promotion_reservations");

            migrationBuilder.DropTable(
                name: "ie_promotion_codes");

            migrationBuilder.DropTable(
                name: "ie_promotion_reservation_statuses");

            migrationBuilder.DropTable(
                name: "ie_promotion_definitions");

            migrationBuilder.DropTable(
                name: "ie_promotion_definition_statuses");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_orders_tenant_id_applied_promotion_code_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_orders_tenant_id_applied_promotion_definition_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "active_promotion_reservation_id",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "applied_promotion_code_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "applied_promotion_definition_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "applied_promotion_display_label_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "post_discount_organizer_directed_total_minor_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "pre_discount_organizer_directed_total_minor_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "promotion_discount_total_minor_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "is_email_verified",
                table: "ie_registration_order_pii");

            migrationBuilder.DropColumn(
                name: "post_discount_line_subtotal_minor_snapshot",
                table: "ie_registration_order_lines");

            migrationBuilder.DropColumn(
                name: "pre_discount_line_subtotal_minor_snapshot",
                table: "ie_registration_order_lines");

            migrationBuilder.DropColumn(
                name: "promotion_discount_amount_minor_snapshot",
                table: "ie_registration_order_lines");
        }
    }
}
