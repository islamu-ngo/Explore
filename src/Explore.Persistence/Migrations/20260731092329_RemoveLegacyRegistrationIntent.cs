using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyRegistrationIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM event_registration_intents)
                    THEN
                        RAISE EXCEPTION 'Cannot remove legacy registration intents while rows exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_email_dispatch_outbox_event_registration_intents_registrati",
                table: "email_dispatch_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_event_registration_intents_ten",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_registrations_event_registration_intents_tenant_id_ev",
                table: "event_registrations");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_sync_outbox_event_registration_intents_registra",
                table: "integration_sync_outbox");

            migrationBuilder.DropTable(
                name: "event_registration_intents");

            migrationBuilder.DropIndex(
                name: "ix_integration_sync_outbox_registration_intent_id",
                table: "integration_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_intent",
                table: "event_registrations");

            migrationBuilder.DropIndex(
                name: "ix_email_dispatch_outbox_registration_intent_id",
                table: "email_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "event_registration_intent_id",
                table: "event_registrations");

            migrationBuilder.RenameColumn(
                name: "registration_intent_id",
                table: "integration_sync_outbox",
                newName: "registration_order_id");

            migrationBuilder.RenameColumn(
                name: "source_event_registration_intent_id",
                table: "event_contact_share_consents",
                newName: "source_registration_order_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_consents_tenant_id_source_event_registr",
                table: "event_contact_share_consents",
                newName: "ix_event_contact_share_consents_tenant_id_source_registration_");

            migrationBuilder.RenameColumn(
                name: "registration_intent_id",
                table: "email_dispatch_outbox",
                newName: "registration_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_sync_outbox_tenant_id_registration_order_id",
                table: "integration_sync_outbox",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_tenant_id_registration_order_id",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_email_dispatch_outbox_registration_orders_tenant_id_registr",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "registration_order_id" },
                principalTable: "registration_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "source_registration_order_id" },
                principalTable: "registration_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_sync_outbox_registration_orders_tenant_id_regis",
                table: "integration_sync_outbox",
                columns: new[] { "tenant_id", "registration_order_id" },
                principalTable: "registration_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM registration_orders)
                        OR EXISTS (SELECT 1 FROM registration_order_lines)
                        OR EXISTS (SELECT 1 FROM registration_order_pii)
                        OR EXISTS (SELECT 1 FROM registration_order_platform_contributions)
                        OR EXISTS (SELECT 1 FROM registration_inventory_holds)
                        OR EXISTS (
                            SELECT 1
                            FROM event_registrations
                            WHERE registration_order_id IS NOT NULL
                                OR registration_order_line_id IS NOT NULL
                                OR registration_participant_id IS NOT NULL
                                OR ticket_type_entitlement_id IS NOT NULL
                                OR entitlement_ordinal IS NOT NULL)
                        OR EXISTS (
                            SELECT 1
                            FROM event_registrations
                            WHERE user_id IS NULL)
                        OR EXISTS (
                            SELECT 1
                            FROM event_registrations
                            WHERE is_deleted = false
                            GROUP BY tenant_id, event_id, event_session_id, user_id
                            HAVING COUNT(*) > 1)
                        OR EXISTS (
                            SELECT 1
                            FROM event_capacity_pools
                            WHERE capacity_hold_policy_id <> 1)
                    THEN
                        RAISE EXCEPTION 'Cannot downgrade registration order cutover while Phase 5 data or incompatible admission rows exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_email_dispatch_outbox_registration_orders_tenant_id_registr",
                table: "email_dispatch_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_integration_sync_outbox_registration_orders_tenant_id_regis",
                table: "integration_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_integration_sync_outbox_tenant_id_registration_order_id",
                table: "integration_sync_outbox");

            migrationBuilder.DropIndex(
                name: "ix_email_dispatch_outbox_tenant_id_registration_order_id",
                table: "email_dispatch_outbox");

            migrationBuilder.RenameColumn(
                name: "registration_order_id",
                table: "integration_sync_outbox",
                newName: "registration_intent_id");

            migrationBuilder.RenameColumn(
                name: "source_registration_order_id",
                table: "event_contact_share_consents",
                newName: "source_event_registration_intent_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_consents_tenant_id_source_registration_",
                table: "event_contact_share_consents",
                newName: "ix_event_contact_share_consents_tenant_id_source_event_registr");

            migrationBuilder.RenameColumn(
                name: "registration_order_id",
                table: "email_dispatch_outbox",
                newName: "registration_intent_id");

            migrationBuilder.AddColumn<Guid>(
                name: "event_registration_intent_id",
                table: "event_registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "event_registration_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_status_id = table.Column<int>(type: "integer", nullable: true),
                    registration_policy_snapshot_id = table.Column<int>(type: "integer", nullable: true),
                    registration_scope_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_registration_intents", x => x.id);
                    table.UniqueConstraint("ak_event_registration_intents_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_registration_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_registration_intents_approval_statuses_approval_statu",
                        column: x => x.approval_status_id,
                        principalTable: "approval_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_event_days_tenant_id_event_id_se",
                        columns: x => new { x.tenant_id, x.event_id, x.selected_event_day_id },
                        principalTable: "event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_event_registration_policies_regi",
                        column: x => x.registration_policy_snapshot_id,
                        principalTable: "event_registration_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_registration_scopes_registration",
                        column: x => x.registration_scope_id,
                        principalTable: "registration_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_registration_intents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_integration_sync_outbox_registration_intent_id",
                table: "integration_sync_outbox",
                column: "registration_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_intent",
                table: "event_registrations",
                columns: new[] { "tenant_id", "event_id", "event_registration_intent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_registration_intent_id",
                table: "email_dispatch_outbox",
                column: "registration_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_approval_status_id",
                table: "event_registration_intents",
                column: "approval_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_registration_policy_snapshot_id",
                table: "event_registration_intents",
                column: "registration_policy_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_registration_scope_id",
                table: "event_registration_intents",
                column: "registration_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_event_day",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "selected_event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_event_user_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id", "registration_scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_tenant_user",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_unique_day_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id", "selected_event_day_id" },
                unique: true,
                filter: "registration_scope_id = 2 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_unique_event_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id" },
                unique: true,
                filter: "registration_scope_id = 1 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_unique_session_selection_scope",
                table: "event_registration_intents",
                columns: new[] { "tenant_id", "event_id", "user_id" },
                unique: true,
                filter: "registration_scope_id = 3 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_registration_intents_user_id",
                table: "event_registration_intents",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_email_dispatch_outbox_event_registration_intents_registrati",
                table: "email_dispatch_outbox",
                column: "registration_intent_id",
                principalTable: "event_registration_intents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_event_registration_intents_ten",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "source_event_registration_intent_id" },
                principalTable: "event_registration_intents",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_registrations_event_registration_intents_tenant_id_ev",
                table: "event_registrations",
                columns: new[] { "tenant_id", "event_id", "event_registration_intent_id" },
                principalTable: "event_registration_intents",
                principalColumns: new[] { "tenant_id", "event_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_integration_sync_outbox_event_registration_intents_registra",
                table: "integration_sync_outbox",
                column: "registration_intent_id",
                principalTable: "event_registration_intents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
