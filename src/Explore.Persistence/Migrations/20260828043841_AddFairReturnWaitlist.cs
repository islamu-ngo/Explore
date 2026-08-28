using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFairReturnWaitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_waitlist_entries",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_policy_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_account_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    commercial_terms_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    admission_entitlement_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    gross_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    refund_funding_mode_id = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    enqueued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    open_registration_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_waitlist_entries", x => x.id);
                    table.UniqueConstraint("ak_event_waitlist_entries_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_waitlist_entries_amount", "gross_minor_units >= 0");
                    table.CheckConstraint("ck_event_waitlist_entries_state", "(status_id IN (1, 2) AND open_registration_order_line_id IS NOT NULL) OR (status_id IN (3, 4) AND open_registration_order_line_id IS NULL)");
                    table.CheckConstraint("ck_event_waitlist_entries_status", "status_id BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_event_waitlist_entries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fair_return_supply_policies",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    offer_lifetime_minutes = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fair_return_supply_policies", x => x.id);
                    table.UniqueConstraint("ak_fair_return_supply_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_supply_policy_lifetime", "offer_lifetime_minutes BETWEEN 5 AND 43200");
                    table.ForeignKey(
                        name: "fk_fair_return_supply_policies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fair_return_supply_units",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_policy_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    commercial_terms_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    admission_entitlement_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    gross_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    refund_funding_mode_id = table.Column<int>(type: "integer", nullable: false),
                    seller_registration_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    bound_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    withdrawn_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fair_return_supply_units", x => x.id);
                    table.UniqueConstraint("ak_fair_return_supply_units_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_supply_units_amount", "gross_minor_units >= 0");
                    table.CheckConstraint("ck_fair_return_supply_units_state", "(status_id = 1 AND bound_at IS NULL AND withdrawn_at IS NULL) OR (status_id = 2 AND bound_at IS NOT NULL AND withdrawn_at IS NULL) OR (status_id = 3 AND withdrawn_at IS NOT NULL)");
                    table.CheckConstraint("ck_fair_return_supply_units_status", "status_id BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "fk_fair_return_supply_units_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fair_return_source_bindings",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fair_return_supply_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_registration_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_account_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    commercial_terms_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    admission_entitlement_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    payment_dispatch_claimed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_substituted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fair_return_source_bindings", x => x.id);
                    table.UniqueConstraint("ak_fair_return_source_bindings_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_source_bindings_amount", "unit_amount_minor >= 0");
                    table.ForeignKey(
                        name: "fk_fair_return_source_bindings_fair_return_supply_units_tenant",
                        columns: x => new { x.tenant_id, x.fair_return_supply_unit_id },
                        principalSchema: "islamu_event",
                        principalTable: "fair_return_supply_units",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fair_return_source_bindings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_waitlist_offers",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_waitlist_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fair_return_supply_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fair_return_source_binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    existing_capacity_hold_id = table.Column<Guid>(type: "uuid", nullable: false),
                    open_event_waitlist_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    offered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_waitlist_offers", x => x.id);
                    table.UniqueConstraint("ak_event_waitlist_offers_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_waitlist_offers_state", "(status_id = 1 AND open_event_waitlist_entry_id IS NOT NULL AND finalized_at IS NULL AND expired_at IS NULL) OR (status_id = 2 AND open_event_waitlist_entry_id IS NULL AND finalized_at IS NULL AND expired_at IS NOT NULL) OR (status_id = 3 AND open_event_waitlist_entry_id IS NULL AND finalized_at IS NOT NULL AND expired_at IS NULL)");
                    table.CheckConstraint("ck_event_waitlist_offers_status", "status_id BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "fk_event_waitlist_offers_event_waitlist_entries_tenant_id_even",
                        columns: x => new { x.tenant_id, x.event_waitlist_entry_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_waitlist_entries",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_waitlist_offers_fair_return_source_bindings_tenant_id",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalSchema: "islamu_event",
                        principalTable: "fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_waitlist_offers_fair_return_supply_units_tenant_id_fa",
                        columns: x => new { x.tenant_id, x.fair_return_supply_unit_id },
                        principalSchema: "islamu_event",
                        principalTable: "fair_return_supply_units",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_waitlist_offers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "waitlist_provider_observations",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fair_return_source_binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(32)", unicode: false, maxLength: 32, nullable: false),
                    provider_object_type = table.Column<string>(type: "character varying(32)", unicode: false, maxLength: 32, nullable: false),
                    provider_object_id_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    provider_observation_id_digest = table.Column<string>(type: "character(44)", unicode: false, fixedLength: true, maxLength: 44, nullable: false),
                    observed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state_code = table.Column<string>(type: "character varying(32)", unicode: false, maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waitlist_provider_observations", x => x.id);
                    table.UniqueConstraint("ak_waitlist_provider_observations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_waitlist_provider_observations_fair_return_source_bindings_",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalSchema: "islamu_event",
                        principalTable: "fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waitlist_provider_observations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "waitlist_refund_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fair_return_source_binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_payment_allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replacement_payment_settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waitlist_refund_intents", x => x.id);
                    table.UniqueConstraint("ak_waitlist_refund_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_waitlist_refund_intents_fair_return_source_bindings_tenant_",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalSchema: "islamu_event",
                        principalTable: "fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waitlist_refund_intents_outbox_messages_outbox_message_id",
                        column: x => x.outbox_message_id,
                        principalSchema: "islamu_event",
                        principalTable: "outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waitlist_refund_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_entries_tenant_id",
                schema: "islamu_event",
                table: "event_waitlist_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_entries_tenant_id_event_id_event_ticket_type",
                schema: "islamu_event",
                table: "event_waitlist_entries",
                columns: new[] { "tenant_id", "event_id", "event_ticket_type_id", "status_id", "priority", "enqueued_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_entries_tenant_id_open_registration_order_li",
                schema: "islamu_event",
                table: "event_waitlist_entries",
                columns: new[] { "tenant_id", "open_registration_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_offers_tenant_id",
                schema: "islamu_event",
                table: "event_waitlist_offers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_offers_tenant_id_event_waitlist_entry_id",
                schema: "islamu_event",
                table: "event_waitlist_offers",
                columns: new[] { "tenant_id", "event_waitlist_entry_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_offers_tenant_id_expires_at_status_id",
                schema: "islamu_event",
                table: "event_waitlist_offers",
                columns: new[] { "tenant_id", "expires_at", "status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_offers_tenant_id_fair_return_source_binding_",
                schema: "islamu_event",
                table: "event_waitlist_offers",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_offers_tenant_id_fair_return_supply_unit_id",
                schema: "islamu_event",
                table: "event_waitlist_offers",
                columns: new[] { "tenant_id", "fair_return_supply_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_waitlist_offers_tenant_id_open_event_waitlist_entry_id",
                schema: "islamu_event",
                table: "event_waitlist_offers",
                columns: new[] { "tenant_id", "open_event_waitlist_entry_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_source_bindings_tenant_id",
                schema: "islamu_event",
                table: "fair_return_source_bindings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_source_bindings_tenant_id_buyer_registration_or",
                schema: "islamu_event",
                table: "fair_return_source_bindings",
                columns: new[] { "tenant_id", "buyer_registration_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_source_bindings_tenant_id_fair_return_supply_un",
                schema: "islamu_event",
                table: "fair_return_source_bindings",
                columns: new[] { "tenant_id", "fair_return_supply_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_supply_policies_tenant_id",
                schema: "islamu_event",
                table: "fair_return_supply_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_supply_policies_tenant_id_event_id_ticket_catal",
                schema: "islamu_event",
                table: "fair_return_supply_policies",
                columns: new[] { "tenant_id", "event_id", "ticket_catalog_version_id", "event_ticket_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_supply_units_tenant_id",
                schema: "islamu_event",
                table: "fair_return_supply_units",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_supply_units_tenant_id_event_id_event_ticket_ty",
                schema: "islamu_event",
                table: "fair_return_supply_units",
                columns: new[] { "tenant_id", "event_id", "event_ticket_type_id", "ticket_catalog_version_id", "purchase_policy_snapshot_id", "currency_code", "commercial_terms_digest", "admission_entitlement_digest", "gross_minor_units", "refund_funding_mode_id", "status_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_supply_units_tenant_id_seller_registration_orde",
                schema: "islamu_event",
                table: "fair_return_supply_units",
                columns: new[] { "tenant_id", "seller_registration_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_provider_observations_tenant_id",
                schema: "islamu_event",
                table: "waitlist_provider_observations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_provider_observations_tenant_id_fair_return_source",
                schema: "islamu_event",
                table: "waitlist_provider_observations",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_provider_observations_tenant_id_provider_code_prov",
                schema: "islamu_event",
                table: "waitlist_provider_observations",
                columns: new[] { "tenant_id", "provider_code", "provider_object_type", "provider_object_id_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_refund_intents_outbox_message_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                column: "outbox_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_refund_intents_tenant_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_refund_intents_tenant_id_fair_return_source_bindin",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_waitlist_offers",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "fair_return_supply_policies",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "waitlist_provider_observations",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "waitlist_refund_intents",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "event_waitlist_entries",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "fair_return_source_bindings",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "fair_return_supply_units",
                schema: "islamu_event");
        }
    }
}
