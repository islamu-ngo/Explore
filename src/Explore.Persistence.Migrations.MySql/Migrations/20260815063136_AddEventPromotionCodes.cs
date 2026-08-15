using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
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
                type: "char(36)",
                nullable: true)
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "applied_promotion_code_id_snapshot",
                table: "ie_registration_orders",
                type: "char(36)",
                nullable: true)
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "applied_promotion_definition_version_id_snapshot",
                table: "ie_registration_orders",
                type: "char(36)",
                nullable: true)
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "applied_promotion_display_label_snapshot",
                table: "ie_registration_orders",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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
                type: "tinyint(1)",
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
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_definition_statuses", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_promotion_reservation_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_reservation_statuses", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_promotion_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    definition_group_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    promotion_definition_status_id = table.Column<int>(type: "int", nullable: false),
                    scope_metadata = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_label = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    eligibility = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    discount_rule = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    starts_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ends_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    total_redemption_limit = table.Column<int>(type: "int", nullable: true),
                    per_verified_purchaser_limit = table.Column<int>(type: "int", nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scope_currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scope_event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scope_ticket_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scope_ticket_catalog_version_number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_definitions", x => x.id);
                    table.UniqueConstraint("ak_promotion_definitions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_promotion_definitions_ie_promotion_definition_sta_698101A5",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_promotion_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    promotion_definition_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scope_metadata = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_label = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    lookup_digest = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lookup_key_version = table.Column<int>(type: "int", nullable: false),
                    retired_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    scope_currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scope_event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scope_ticket_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scope_ticket_catalog_version_number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_codes", x => x.id);
                    table.UniqueConstraint("ak_promotion_codes_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_promotion_codes_ie_promotion_definitions_tenant_i_F60F1EC8",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_promotion_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    promotion_definition_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    promotion_code_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    promotion_reservation_status_id = table.Column<int>(type: "int", nullable: false),
                    order_reservation_slot = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    reserved_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    released_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expired_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_promotion_reservations", x => x.id);
                    table.UniqueConstraint("ak_promotion_reservations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_promotion_reservation_active_slot", "(promotion_reservation_status_id = 1 AND order_reservation_slot = '00000000-0000-0000-0000-000000000000') OR (promotion_reservation_status_id <> 1 AND order_reservation_slot = id)");
                    table.CheckConstraint("ck_promotion_reservation_status_timestamps", "(promotion_reservation_status_id = 1 AND consumed_at_utc IS NULL AND released_at_utc IS NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 2 AND consumed_at_utc IS NOT NULL AND released_at_utc IS NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 3 AND consumed_at_utc IS NULL AND released_at_utc IS NOT NULL AND expired_at_utc IS NULL) OR (promotion_reservation_status_id = 4 AND consumed_at_utc IS NULL AND released_at_utc IS NULL AND expired_at_utc IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ie_promotion_reservations_ie_promotion_codes_tenant__05C256D0",
                        columns: x => new { x.tenant_id, x.promotion_code_id },
                        principalTable: "ie_promotion_codes",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_promotion_reservations_ie_promotion_definitions_t_1E4BA3FF",
                        columns: x => new { x.tenant_id, x.promotion_definition_version_id },
                        principalTable: "ie_promotion_definitions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_promotion_reservations_ie_promotion_reservation_s_B2F0A166",
                        column: x => x.promotion_reservation_status_id,
                        principalTable: "ie_promotion_reservation_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_promotion_reservations_ie_registration_orders_ten_60FE2EC4",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_orders_tenant_id_applied_promotion_c_BA3F5999",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_code_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_orders_tenant_id_applied_promotion_d_FE845939",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_definition_version_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_promotion_codes_tenant_id_promotion_definition_ve_41FF9215",
                table: "ie_promotion_codes",
                columns: new[] { "tenant_id", "promotion_definition_version_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_promotion_codes_tenant_id_scope_event_id_scope_ti_78AC93A6",
                table: "ie_promotion_codes",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_promotion_codes_tenant_id_scope_event_id_scope_ti_D872FBD4",
                table: "ie_promotion_codes",
                columns: new[] { "tenant_id", "scope_event_id", "scope_ticket_catalog_version_id", "lookup_key_version" });

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
                name: "IX_ie_promotion_definitions_tenant_id_definition_group__62F85AAD",
                table: "ie_promotion_definitions",
                columns: new[] { "tenant_id", "definition_group_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_promotion_definitions_tenant_id_scope_event_id_sc_CB1458DD",
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
                name: "IX_ie_promotion_reservations_registration_order_id_orde_460CBDBF",
                table: "ie_promotion_reservations",
                columns: new[] { "registration_order_id", "order_reservation_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservations_tenant_id_promotion_code_id",
                table: "ie_promotion_reservations",
                columns: new[] { "tenant_id", "promotion_code_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_promotion_reservations_tenant_id_promotion_defini_D6D130AD",
                table: "ie_promotion_reservations",
                columns: new[] { "tenant_id", "promotion_definition_version_id", "promotion_reservation_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_promotion_reservations_tenant_id_registration_order_id",
                table: "ie_promotion_reservations",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_orders_ie_promotion_codes_tenant_id__9A0F2576",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "applied_promotion_code_id_snapshot" },
                principalTable: "ie_promotion_codes",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_orders_ie_promotion_definitions_tena_9D9AB163",
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
                name: "FK_ie_registration_orders_ie_promotion_codes_tenant_id__9A0F2576",
                table: "ie_registration_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_orders_ie_promotion_definitions_tena_9D9AB163",
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
                name: "IX_ie_registration_orders_tenant_id_applied_promotion_c_BA3F5999",
                table: "ie_registration_orders");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_orders_tenant_id_applied_promotion_d_FE845939",
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
