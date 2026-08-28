using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddFairReturnWaitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_event_waitlist_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_ticket_type_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    ticket_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    purchase_policy_snapshot_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_line_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    participant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    buyer_account_user_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    commercial_terms_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    admission_entitlement_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gross_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    refund_funding_mode_id = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    enqueued_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    open_registration_order_line_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    status_id = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_ie_event_waitlist_entries", x => x.id);
                    table.UniqueConstraint("ak_event_waitlist_entries_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_waitlist_entries_amount", "gross_minor_units >= 0");
                    table.CheckConstraint("ck_event_waitlist_entries_state", "(status_id IN (1, 2) AND open_registration_order_line_id IS NOT NULL) OR (status_id IN (3, 4) AND open_registration_order_line_id IS NULL)");
                    table.CheckConstraint("ck_event_waitlist_entries_status", "status_id BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_ie_event_waitlist_entries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_fair_return_supply_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    ticket_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_ticket_type_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    offer_lifetime_minutes = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_ie_fair_return_supply_policies", x => x.id);
                    table.UniqueConstraint("ak_fair_return_supply_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_supply_policy_lifetime", "offer_lifetime_minutes BETWEEN 5 AND 43200");
                    table.ForeignKey(
                        name: "fk_ie_fair_return_supply_policies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_fair_return_supply_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_ticket_type_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    ticket_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    purchase_policy_snapshot_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    commercial_terms_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    admission_entitlement_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gross_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    refund_funding_mode_id = table.Column<int>(type: "int", nullable: false),
                    seller_registration_order_line_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    bound_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    withdrawn_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("pk_ie_fair_return_supply_units", x => x.id);
                    table.UniqueConstraint("ak_fair_return_supply_units_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_supply_units_amount", "gross_minor_units >= 0");
                    table.CheckConstraint("ck_fair_return_supply_units_state", "(status_id = 1 AND bound_at IS NULL AND withdrawn_at IS NULL) OR (status_id = 2 AND bound_at IS NOT NULL AND withdrawn_at IS NULL) OR (status_id = 3 AND withdrawn_at IS NOT NULL)");
                    table.CheckConstraint("ck_fair_return_supply_units_status", "status_id BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "fk_ie_fair_return_supply_units_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_fair_return_source_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    fair_return_supply_unit_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    buyer_registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    buyer_registration_order_line_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    buyer_account_user_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    unit_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    commercial_terms_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    admission_entitlement_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payment_dispatch_claimed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    source_substituted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("pk_ie_fair_return_source_bindings", x => x.id);
                    table.UniqueConstraint("ak_fair_return_source_bindings_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_source_bindings_amount", "unit_amount_minor >= 0");
                    table.ForeignKey(
                        name: "FK_ie_fair_return_source_bindings_ie_fair_return_supply_E3B577CD",
                        columns: x => new { x.tenant_id, x.fair_return_supply_unit_id },
                        principalTable: "ie_fair_return_supply_units",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_fair_return_source_bindings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_waitlist_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_waitlist_entry_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    fair_return_supply_unit_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    fair_return_source_binding_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    existing_capacity_hold_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    open_event_waitlist_entry_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    offered_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    finalized_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expired_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status_id = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_ie_event_waitlist_offers", x => x.id);
                    table.UniqueConstraint("ak_event_waitlist_offers_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_waitlist_offers_state", "(status_id = 1 AND open_event_waitlist_entry_id IS NOT NULL AND finalized_at IS NULL AND expired_at IS NULL) OR (status_id = 2 AND open_event_waitlist_entry_id IS NULL AND finalized_at IS NULL AND expired_at IS NOT NULL) OR (status_id = 3 AND open_event_waitlist_entry_id IS NULL AND finalized_at IS NOT NULL AND expired_at IS NULL)");
                    table.CheckConstraint("ck_event_waitlist_offers_status", "status_id BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_ie_event_waitlist_offers_ie_event_waitlist_entries_t_CB930199",
                        columns: x => new { x.tenant_id, x.event_waitlist_entry_id },
                        principalTable: "ie_event_waitlist_entries",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_event_waitlist_offers_ie_fair_return_source_bindi_23F4BBDB",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalTable: "ie_fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_event_waitlist_offers_ie_fair_return_supply_units_1194AB75",
                        columns: x => new { x.tenant_id, x.fair_return_supply_unit_id },
                        principalTable: "ie_fair_return_supply_units",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_waitlist_offers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_waitlist_provider_observations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    fair_return_source_binding_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    provider_code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_object_type = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_object_id_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_observation_id_digest = table.Column<string>(type: "char(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    observed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    state_code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_waitlist_provider_observations", x => x.id);
                    table.UniqueConstraint("ak_waitlist_provider_observations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_waitlist_provider_observations_ie_fair_return_sou_4706D52B",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalTable: "ie_fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_waitlist_provider_observations_ie_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_waitlist_refund_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    fair_return_source_binding_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    original_payment_allocation_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    outbox_message_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    replacement_payment_settled_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_waitlist_refund_intents", x => x.id);
                    table.UniqueConstraint("ak_waitlist_refund_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_waitlist_refund_intents_ie_fair_return_source_bin_B2E9FDEF",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalTable: "ie_fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_waitlist_refund_intents_ie_outbox_messages_outbox_9D37D0B7",
                        column: x => x.outbox_message_id,
                        principalTable: "ie_outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_waitlist_refund_intents_ie_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_waitlist_entries_tenant_id",
                table: "ie_event_waitlist_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_event_waitlist_entries_tenant_id_event_id_event_t_4DC4671C",
                table: "ie_event_waitlist_entries",
                columns: new[] { "tenant_id", "event_id", "event_ticket_type_id", "status_id", "priority", "enqueued_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_event_waitlist_entries_tenant_id_open_registratio_5914F3DA",
                table: "ie_event_waitlist_entries",
                columns: new[] { "tenant_id", "open_registration_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_waitlist_offers_tenant_id",
                table: "ie_event_waitlist_offers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_waitlist_offers_tenant_id_event_waitlist_entry_id",
                table: "ie_event_waitlist_offers",
                columns: new[] { "tenant_id", "event_waitlist_entry_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_waitlist_offers_tenant_id_expires_at_status_id",
                table: "ie_event_waitlist_offers",
                columns: new[] { "tenant_id", "expires_at", "status_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_event_waitlist_offers_tenant_id_fair_return_sourc_721C8C67",
                table: "ie_event_waitlist_offers",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_waitlist_offers_tenant_id_fair_return_supply_unit_id",
                table: "ie_event_waitlist_offers",
                columns: new[] { "tenant_id", "fair_return_supply_unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_event_waitlist_offers_tenant_id_open_event_waitli_5642EBDE",
                table: "ie_event_waitlist_offers",
                columns: new[] { "tenant_id", "open_event_waitlist_entry_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_fair_return_source_bindings_tenant_id",
                table: "ie_fair_return_source_bindings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_source_bindings_tenant_id_buyer_regis_9115DF71",
                table: "ie_fair_return_source_bindings",
                columns: new[] { "tenant_id", "buyer_registration_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_source_bindings_tenant_id_fair_return_BFFBEABA",
                table: "ie_fair_return_source_bindings",
                columns: new[] { "tenant_id", "fair_return_supply_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_fair_return_supply_policies_tenant_id",
                table: "ie_fair_return_supply_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_supply_policies_tenant_id_event_id_ti_7945615B",
                table: "ie_fair_return_supply_policies",
                columns: new[] { "tenant_id", "event_id", "ticket_catalog_version_id", "event_ticket_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_fair_return_supply_units_tenant_id",
                table: "ie_fair_return_supply_units",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_supply_units_tenant_id_event_id_event_58DF906B",
                table: "ie_fair_return_supply_units",
                columns: new[] { "tenant_id", "event_id", "event_ticket_type_id", "ticket_catalog_version_id", "purchase_policy_snapshot_id", "currency_code", "commercial_terms_digest", "admission_entitlement_digest", "gross_minor_units", "refund_funding_mode_id", "status_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_supply_units_tenant_id_seller_registr_891F34BB",
                table: "ie_fair_return_supply_units",
                columns: new[] { "tenant_id", "seller_registration_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_waitlist_provider_observations_tenant_id",
                table: "ie_waitlist_provider_observations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_waitlist_provider_observations_tenant_id_fair_ret_A8F1EB5A",
                table: "ie_waitlist_provider_observations",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_waitlist_provider_observations_tenant_id_provider_380C9FD5",
                table: "ie_waitlist_provider_observations",
                columns: new[] { "tenant_id", "provider_code", "provider_object_type", "provider_object_id_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_waitlist_refund_intents_outbox_message_id",
                table: "ie_waitlist_refund_intents",
                column: "outbox_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_waitlist_refund_intents_tenant_id",
                table: "ie_waitlist_refund_intents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_waitlist_refund_intents_tenant_id_fair_return_sou_49CE39D5",
                table: "ie_waitlist_refund_intents",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_event_waitlist_offers");

            migrationBuilder.DropTable(
                name: "ie_fair_return_supply_policies");

            migrationBuilder.DropTable(
                name: "ie_waitlist_provider_observations");

            migrationBuilder.DropTable(
                name: "ie_waitlist_refund_intents");

            migrationBuilder.DropTable(
                name: "ie_event_waitlist_entries");

            migrationBuilder.DropTable(
                name: "ie_fair_return_source_bindings");

            migrationBuilder.DropTable(
                name: "ie_fair_return_supply_units");
        }
    }
}
