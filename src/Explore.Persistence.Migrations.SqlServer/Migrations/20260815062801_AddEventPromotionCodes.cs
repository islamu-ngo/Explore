using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPromotionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "active_promotion_reservation_id",
                schema: "islamu_event",
                table: "registration_orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "applied_promotion_code_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "applied_promotion_definition_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "applied_promotion_display_label_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "post_discount_organizer_directed_total_minor_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "pre_discount_organizer_directed_total_minor_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE islamu_event.registration_orders SET pre_discount_organizer_directed_total_minor_snapshot = organizer_directed_total_minor_snapshot, post_discount_organizer_directed_total_minor_snapshot = organizer_directed_total_minor_snapshot;");

            migrationBuilder.AddColumn<long>(
                name: "promotion_discount_total_minor_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "is_email_verified",
                schema: "islamu_event",
                table: "registration_order_pii",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "post_discount_line_subtotal_minor_snapshot",
                schema: "islamu_event",
                table: "registration_order_lines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "pre_discount_line_subtotal_minor_snapshot",
                schema: "islamu_event",
                table: "registration_order_lines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE islamu_event.registration_order_lines SET pre_discount_line_subtotal_minor_snapshot = line_subtotal_snapshot, post_discount_line_subtotal_minor_snapshot = line_subtotal_snapshot;");

            migrationBuilder.AddColumn<long>(
                name: "promotion_discount_amount_minor_snapshot",
                schema: "islamu_event",
                table: "registration_order_lines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "promotion_definition_statuses",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_definition_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_reservation_statuses",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_reservation_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_definitions",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    definition_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    promotion_definition_status_id = table.Column<int>(type: "int", nullable: false),
                    scope_metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    eligibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    discount_rule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    starts_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ends_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    total_redemption_limit = table.Column<int>(type: "int", nullable: true),
                    per_verified_purchaser_limit = table.Column<int>(type: "int", nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    scope_currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    scope_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope_ticket_catalog_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope_ticket_catalog_version_number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_definitions", x => x.id);
                    table.UniqueConstraint("ak_promotion_definitions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_promotion_definitions_promotion_definition_statuses_promotion_definition_status_id",
                        column: x => x.promotion_definition_status_id,
                        principalSchema: "islamu_event",
                        principalTable: "promotion_definition_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_definitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_codes",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promotion_definition_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope_metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_label = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    lookup_digest = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    lookup_key_version = table.Column<int>(type: "int", nullable: false),
                    retired_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    scope_currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    scope_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope_ticket_catalog_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope_ticket_catalog_version_number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_codes", x => x.id);
                    table.UniqueConstraint("ak_promotion_codes_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_promotion_codes_promotion_definitions_tenant_id_promotion_definition_version_id",
                        columns: x => new { x.tenant_id, x.promotion_definition_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "promotion_definitions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_codes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_reservations",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promotion_definition_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promotion_code_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promotion_reservation_status_id = table.Column<int>(type: "int", nullable: false),
                    order_reservation_slot = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reserved_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    released_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    expired_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_reservations", x => x.id);
                    table.UniqueConstraint("ak_promotion_reservations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_promotion_reservation_active_slot", "(promotion_reservation_status_id = 1 AND order_reservation_slot = '00000000-0000-0000-0000-000000000000') OR (promotion_reservation_status_id <> 1 AND order_reservation_slot = id)");
                    table.CheckConstraint("ck_promotion_reservation_status_timestamps", "(promotion_reservation_status_id = 1 AND consumed_at_utc IS NULL AND released_at_utc IS NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 2 AND consumed_at_utc IS NOT NULL AND released_at_utc IS NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 3 AND consumed_at_utc IS NULL AND released_at_utc IS NOT NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 4 AND consumed_at_utc IS NULL AND released_at_utc IS NULL AND expired_at_utc IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_promotion_reservations_promotion_codes_tenant_id_promotion_code_id",
                        columns: x => new { x.tenant_id, x.promotion_code_id },
                        principalSchema: "islamu_event",
                        principalTable: "promotion_codes",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_reservations_promotion_definitions_tenant_id_promotion_definition_version_id",
                        columns: x => new { x.tenant_id, x.promotion_definition_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "promotion_definitions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_reservations_promotion_reservation_statuses_promotion_reservation_status_id",
                        column: x => x.promotion_reservation_status_id,
                        principalSchema: "islamu_event",
                        principalTable: "promotion_reservation_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_reservations_registration_orders_tenant_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promotion_reservations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_applied_promotion_code_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_code_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_applied_promotion_definition_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_definition_version_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_codes_tenant_id_promotion_definition_version_id_is_active",
                schema: "islamu_event",
                table: "promotion_codes",
                columns: new[] { "tenant_id", "promotion_definition_version_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_codes_tenant_id_scope_event_id_scope_ticket_catalog_version_id_lookup_key_version",
                schema: "islamu_event",
                table: "promotion_codes",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "lookup_key_version" });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_codes_tenant_id_scope_event_id_scope_ticket_catalog_version_id_lookup_key_version_lookup_digest",
                schema: "islamu_event",
                table: "promotion_codes",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_definition_statuses_master_code",
                schema: "islamu_event",
                table: "promotion_definition_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_definitions_promotion_definition_status_id",
                schema: "islamu_event",
                table: "promotion_definitions",
                column: "promotion_definition_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_definitions_tenant_id_definition_group_id_version_number",
                schema: "islamu_event",
                table: "promotion_definitions",
                columns: new[] { "tenant_id", "definition_group_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_definitions_tenant_id_scope_event_id_scope_ticket_catalog_version_id_promotion_definition_status_id",
                schema: "islamu_event",
                table: "promotion_definitions",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "promotion_definition_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_reservation_statuses_master_code",
                schema: "islamu_event",
                table: "promotion_reservation_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_reservations_promotion_reservation_status_id",
                schema: "islamu_event",
                table: "promotion_reservations",
                column: "promotion_reservation_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_reservations_registration_order_id_order_reservation_slot",
                schema: "islamu_event",
                table: "promotion_reservations",
                columns: new[] { "registration_order_id", "order_reservation_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_reservations_tenant_id_promotion_code_id",
                schema: "islamu_event",
                table: "promotion_reservations",
                columns: new[] { "tenant_id", "promotion_code_id" });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_reservations_tenant_id_promotion_definition_version_id_promotion_reservation_status_id",
                schema: "islamu_event",
                table: "promotion_reservations",
                columns: new[] { "tenant_id", "promotion_definition_version_id", "promotion_reservation_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_promotion_reservations_tenant_id_registration_order_id",
                schema: "islamu_event",
                table: "promotion_reservations",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_registration_orders_promotion_codes_tenant_id_applied_promotion_code_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_code_id_snapshot" },
                principalSchema: "islamu_event",
                principalTable: "promotion_codes",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_orders_promotion_definitions_tenant_id_applied_promotion_definition_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_definition_version_id_snapshot" },
                principalSchema: "islamu_event",
                principalTable: "promotion_definitions",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_orders_promotion_codes_tenant_id_applied_promotion_code_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_orders_promotion_definitions_tenant_id_applied_promotion_definition_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropTable(
                name: "promotion_reservations",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "promotion_codes",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "promotion_reservation_statuses",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "promotion_definitions",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "promotion_definition_statuses",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_registration_orders_tenant_id_applied_promotion_code_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropIndex(
                name: "ix_registration_orders_tenant_id_applied_promotion_definition_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "active_promotion_reservation_id",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "applied_promotion_code_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "applied_promotion_definition_version_id_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "applied_promotion_display_label_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "post_discount_organizer_directed_total_minor_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "pre_discount_organizer_directed_total_minor_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "promotion_discount_total_minor_snapshot",
                schema: "islamu_event",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "is_email_verified",
                schema: "islamu_event",
                table: "registration_order_pii");

            migrationBuilder.DropColumn(
                name: "post_discount_line_subtotal_minor_snapshot",
                schema: "islamu_event",
                table: "registration_order_lines");

            migrationBuilder.DropColumn(
                name: "pre_discount_line_subtotal_minor_snapshot",
                schema: "islamu_event",
                table: "registration_order_lines");

            migrationBuilder.DropColumn(
                name: "promotion_discount_amount_minor_snapshot",
                schema: "islamu_event",
                table: "registration_order_lines");
        }
    }
}
