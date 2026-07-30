using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacityHoldPolicyLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "event_registrations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "entitlement_ordinal",
                table: "event_registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "registration_order_id",
                table: "event_registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "registration_order_line_id",
                table: "event_registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "registration_participant_id",
                table: "event_registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ticket_type_entitlement_id",
                table: "event_registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_ticket_type_entitlements_tenant_id_id",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "booking_party_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_party_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capacity_hold_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_capacity_hold_policies", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "capacity_hold_policies",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "NO_HOLD_UNTIL_READY", "No hold until ready", null },
                    { 2, "TIMED_HOLD_ON_SELECTION", "Timed hold on selection", null },
                    { 3, "APPROVAL_NO_HOLD", "Approval without hold", null },
                    { 4, "WAITLIST_WHEN_FULL", "Waitlist when full", null }
                });

            migrationBuilder.AddColumn<int>(
                name: "capacity_hold_policy_id",
                table: "event_capacity_pools",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "registration_inventory_hold_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_inventory_hold_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_order_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_order_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchaser_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_party_type_id = table.Column<int>(type: "integer", nullable: false),
                    registration_order_status_id = table.Column<int>(type: "integer", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participation_configuration_version_snapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    participation_handling_mode_id_snapshot = table.Column<int>(type: "integer", nullable: false),
                    advance_registration_obligation_id_snapshot = table.Column<int>(type: "integer", nullable: false),
                    identity_access_mode_id_snapshot = table.Column<int>(type: "integer", nullable: true),
                    guest_recovery_policy_snapshot = table.Column<int>(type: "integer", nullable: true),
                    registration_order_participation_configuration_version_snapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_workflow_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_access_token_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    organizer_directed_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    organizer_earnings_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    platform_contribution_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    total_due_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_orders", x => x.id);
                    table.UniqueConstraint("ak_registration_orders_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_registration_orders_booking_party_types_booking_party_type_",
                        column: x => x.booking_party_type_id,
                        principalTable: "booking_party_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_orders_event_ticket_catalog_versions_tenant_id",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalTable: "event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_orders_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_orders_registration_order_statuses_registratio",
                        column: x => x.registration_order_status_id,
                        principalTable: "registration_order_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_orders_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_inventory_holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capacity_pool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    registration_inventory_hold_status_id = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_inventory_holds", x => x.id);
                    table.UniqueConstraint("ak_registration_inventory_holds_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_registration_inventory_holds_event_capacity_pools_tenant_id",
                        columns: x => new { x.tenant_id, x.capacity_pool_id },
                        principalTable: "event_capacity_pools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_inventory_holds_event_ticket_types_tenant_id_t",
                        columns: x => new { x.tenant_id, x.ticket_type_id },
                        principalTable: "event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_inventory_holds_registration_inventory_hold_st",
                        column: x => x.registration_inventory_hold_status_id,
                        principalTable: "registration_inventory_hold_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_inventory_holds_registration_orders_tenant_id_",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_inventory_holds_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_order_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_amount_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    chosen_unit_price_amount_snapshot = table.Column<long>(type: "bigint", nullable: true),
                    currency_code_snapshot = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    line_subtotal_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    ticket_type_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ticket_pricing_mode_snapshot = table.Column<int>(type: "integer", nullable: false),
                    minimum_price_amount_snapshot = table.Column<long>(type: "bigint", nullable: true),
                    suggested_price_amount_snapshot = table.Column<long>(type: "bigint", nullable: true),
                    ticket_catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_fee_policy_version_snapshot = table.Column<int>(type: "integer", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_order_lines", x => x.id);
                    table.UniqueConstraint("ak_registration_order_lines_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_registration_order_lines_event_ticket_catalog_versions_tena",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalTable: "event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_lines_event_ticket_types_tenant_id_ticke",
                        columns: x => new { x.tenant_id, x.ticket_type_id },
                        principalTable: "event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_lines_registration_orders_tenant_id_regi",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_order_pii",
                columns: table => new
                {
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    organization_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_order_pii", x => x.registration_order_id);
                    table.UniqueConstraint("ak_registration_order_pii_tenant_id_registration_order_id", x => new { x.tenant_id, x.registration_order_id });
                    table.ForeignKey(
                        name: "fk_registration_order_pii_registration_orders_tenant_id_regist",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_order_platform_contributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_contribution_setting_id_snapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_contribution_setting_version_snapshot = table.Column<int>(type: "integer", nullable: false),
                    contribution_basis_points_snapshot = table.Column<int>(type: "integer", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_order_platform_contributions", x => x.id);
                    table.UniqueConstraint("ak_registration_order_platform_contributions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_registration_order_platform_contributions_registration_orde",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_tenant_id_registration_order_id",
                table: "event_registrations",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registrations_tenant_id_ticket_type_entitlement_id",
                table: "event_registrations",
                columns: new[] { "tenant_id", "ticket_type_entitlement_id" });

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_order_admission",
                table: "event_registrations",
                columns: new[] { "tenant_id", "registration_order_line_id", "ticket_type_entitlement_id", "event_session_id", "entitlement_ordinal" },
                unique: true,
                filter: "registration_order_line_id IS NOT NULL AND ticket_type_entitlement_id IS NOT NULL AND entitlement_ordinal IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations",
                columns: new[] { "tenant_id", "event_id", "event_session_id", "user_id" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_capacity_pools_capacity_hold_policy_id",
                table: "event_capacity_pools",
                column: "capacity_hold_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_party_types_master_code",
                table: "booking_party_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_capacity_hold_policies_master_code",
                table: "capacity_hold_policies",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_inventory_hold_statuses_master_code",
                table: "registration_inventory_hold_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_inventory_holds_registration_inventory_hold_st",
                table: "registration_inventory_holds",
                column: "registration_inventory_hold_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_inventory_holds_tenant_id_capacity_pool_id_reg",
                table: "registration_inventory_holds",
                columns: new[] { "tenant_id", "capacity_pool_id", "registration_inventory_hold_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_inventory_holds_tenant_id_registration_invento",
                table: "registration_inventory_holds",
                columns: new[] { "tenant_id", "registration_inventory_hold_status_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_inventory_holds_tenant_id_registration_order_id",
                table: "registration_inventory_holds",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_inventory_holds_tenant_id_ticket_type_id",
                table: "registration_inventory_holds",
                columns: new[] { "tenant_id", "ticket_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_lines_tenant_id_registration_order_id_ti",
                table: "registration_order_lines",
                columns: new[] { "tenant_id", "registration_order_id", "ticket_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_lines_tenant_id_ticket_catalog_version_id",
                table: "registration_order_lines",
                columns: new[] { "tenant_id", "ticket_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_lines_tenant_id_ticket_type_id",
                table: "registration_order_lines",
                columns: new[] { "tenant_id", "ticket_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_pii_tenant_id_normalized_email",
                table: "registration_order_pii",
                columns: new[] { "tenant_id", "normalized_email" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_platform_contributions_tenant_id_registr",
                table: "registration_order_platform_contributions",
                columns: new[] { "tenant_id", "registration_order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_statuses_master_code",
                table: "registration_order_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_booking_party_type_id",
                table: "registration_orders",
                column: "booking_party_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_registration_order_status_id",
                table: "registration_orders",
                column: "registration_order_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_event_id_registration_order_s",
                table: "registration_orders",
                columns: new[] { "tenant_id", "event_id", "registration_order_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_expires_at",
                table: "registration_orders",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_ticket_catalog_version_id",
                table: "registration_orders",
                columns: new[] { "tenant_id", "ticket_catalog_version_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_event_capacity_pools_capacity_hold_policies_capacity_hold_p",
                table: "event_capacity_pools",
                column: "capacity_hold_policy_id",
                principalTable: "capacity_hold_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_registrations_registration_order_lines_tenant_id_regi",
                table: "event_registrations",
                columns: new[] { "tenant_id", "registration_order_line_id" },
                principalTable: "registration_order_lines",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_registrations_registration_orders_tenant_id_registrat",
                table: "event_registrations",
                columns: new[] { "tenant_id", "registration_order_id" },
                principalTable: "registration_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_registrations_ticket_type_entitlements_tenant_id_tick",
                table: "event_registrations",
                columns: new[] { "tenant_id", "ticket_type_entitlement_id" },
                principalTable: "ticket_type_entitlements",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_capacity_pools_capacity_hold_policies_capacity_hold_p",
                table: "event_capacity_pools");

            migrationBuilder.DropForeignKey(
                name: "fk_event_registrations_registration_order_lines_tenant_id_regi",
                table: "event_registrations");

            migrationBuilder.DropForeignKey(
                name: "fk_event_registrations_registration_orders_tenant_id_registrat",
                table: "event_registrations");

            migrationBuilder.DropForeignKey(
                name: "fk_event_registrations_ticket_type_entitlements_tenant_id_tick",
                table: "event_registrations");

            migrationBuilder.DropTable(
                name: "capacity_hold_policies");

            migrationBuilder.DropTable(
                name: "registration_inventory_holds");

            migrationBuilder.DropTable(
                name: "registration_order_lines");

            migrationBuilder.DropTable(
                name: "registration_order_pii");

            migrationBuilder.DropTable(
                name: "registration_order_platform_contributions");

            migrationBuilder.DropTable(
                name: "registration_inventory_hold_statuses");

            migrationBuilder.DropTable(
                name: "registration_orders");

            migrationBuilder.DropTable(
                name: "booking_party_types");

            migrationBuilder.DropTable(
                name: "registration_order_statuses");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_ticket_type_entitlements_tenant_id_id",
                table: "ticket_type_entitlements");

            migrationBuilder.DropIndex(
                name: "ix_event_registrations_tenant_id_registration_order_id",
                table: "event_registrations");

            migrationBuilder.DropIndex(
                name: "ix_event_registrations_tenant_id_ticket_type_entitlement_id",
                table: "event_registrations");

            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_order_admission",
                table: "event_registrations");

            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations");

            migrationBuilder.DropIndex(
                name: "ix_event_capacity_pools_capacity_hold_policy_id",
                table: "event_capacity_pools");

            migrationBuilder.DropColumn(
                name: "entitlement_ordinal",
                table: "event_registrations");

            migrationBuilder.DropColumn(
                name: "registration_order_id",
                table: "event_registrations");

            migrationBuilder.DropColumn(
                name: "registration_order_line_id",
                table: "event_registrations");

            migrationBuilder.DropColumn(
                name: "registration_participant_id",
                table: "event_registrations");

            migrationBuilder.DropColumn(
                name: "ticket_type_entitlement_id",
                table: "event_registrations");

            migrationBuilder.DropColumn(
                name: "capacity_hold_policy_id",
                table: "event_capacity_pools");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "event_registrations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations",
                columns: new[] { "tenant_id", "event_id", "event_session_id", "user_id" },
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
