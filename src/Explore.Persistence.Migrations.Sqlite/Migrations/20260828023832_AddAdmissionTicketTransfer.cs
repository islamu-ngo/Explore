using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionTicketTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "holder_subject_user_id",
                table: "ie_admission_tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "transfer_hop_count",
                table: "ie_admission_tickets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "lookup_digest",
                table: "ie_admission_recovery_capabilities",
                type: "TEXT",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldFixedLength: true,
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "locator_digest",
                table: "ie_admission_recovery_capabilities",
                type: "TEXT",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldFixedLength: true,
                oldMaxLength: 32);

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    open_admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    from_participant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    to_participant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    recipient_subject_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    offer_operation_key = table.Column<Guid>(type: "TEXT", nullable: false),
                    capability_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 44, nullable: false),
                    transfer_hop = table.Column<int>(type: "INTEGER", nullable: false),
                    credential_generation = table.Column<int>(type: "INTEGER", nullable: false),
                    accepted_credential_generation = table.Column<int>(type: "INTEGER", nullable: true),
                    status_id = table.Column<int>(type: "INTEGER", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    offered_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    capability_consumed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    accepted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    expired_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_transfers", x => x.id);
                    table.UniqueConstraint("ak_admission_ticket_transfers_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_ticket_transfers_positive", "transfer_hop > 0 AND credential_generation > 0 AND (accepted_credential_generation IS NULL OR accepted_credential_generation = credential_generation + 1)");
                    table.CheckConstraint("ck_admission_ticket_transfers_status", "status_id BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_admission_ticket_transfers_terminal_facts", "(status_id = 1 AND accepted_at IS NULL AND cancelled_at IS NULL AND expired_at IS NULL AND capability_consumed_at IS NULL AND accepted_credential_generation IS NULL AND to_participant_id IS NULL AND recipient_subject_user_id IS NULL) OR (status_id = 2 AND accepted_at IS NOT NULL AND capability_consumed_at IS NOT NULL AND accepted_credential_generation IS NOT NULL AND to_participant_id IS NOT NULL AND recipient_subject_user_id IS NOT NULL AND cancelled_at IS NULL AND expired_at IS NULL) OR (status_id = 3 AND cancelled_at IS NOT NULL AND accepted_at IS NULL AND expired_at IS NULL) OR (status_id = 4 AND expired_at IS NOT NULL AND accepted_at IS NULL AND cancelled_at IS NULL)");
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_transfers_ie_admission_tickets_tenant_id_admission_ticket_id",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_transfers_registration_participants_tenant_id_registration_order_id_from_participant_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.from_participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_transfers_registration_participants_tenant_id_registration_order_id_to_participant_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.to_participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_transfers_registration_ticket_assignments_tenant_id_registration_order_id_registration_ticket_assignment_id_registration_order_line_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_transfers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_transfers_users_recipient_subject_user_id",
                        column: x => x.recipient_subject_user_id,
                        principalTable: "ie_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_ticket_transfer_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_ticket_type_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    maximum_hops = table.Column<int>(type: "INTEGER", nullable: false),
                    offer_lifetime_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    cutoff_minutes_before_event = table.Column<int>(type: "INTEGER", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticket_transfer_policies", x => x.id);
                    table.UniqueConstraint("ak_ticket_transfer_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_ticket_transfer_policies_bounds", "maximum_hops BETWEEN 1 AND 100 AND offer_lifetime_minutes BETWEEN 5 AND 43200 AND cutoff_minutes_before_event BETWEEN 0 AND 525600");
                    table.ForeignKey(
                        name: "fk_ie_ticket_transfer_policies_ie_event_ticket_catalog_versions_tenant_id_ticket_catalog_version_id",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalTable: "ie_event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_ticket_transfer_policies_ie_event_ticket_types_tenant_id_event_ticket_type_id",
                        columns: x => new { x.tenant_id, x.event_ticket_type_id },
                        principalTable: "ie_event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_ticket_transfer_policies_ie_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_admission_transfer_delivery_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_transfer_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_transfer_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_transfer_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_ie_admission_transfer_delivery_intents_ie_admission_ticket_transfers_tenant_id_admission_ticket_transfer_id",
                        columns: x => new { x.tenant_id, x.admission_ticket_transfer_id },
                        principalTable: "ie_admission_ticket_transfers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_transfer_delivery_intents_outbox_messages_outbox_message_id",
                        column: x => x.outbox_message_id,
                        principalTable: "ie_outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_transfer_delivery_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_admission_tickets_transfer_hops",
                table: "ie_admission_tickets",
                sql: "transfer_hop_count >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_transfers_recipient_subject_user_id",
                table: "ie_admission_ticket_transfers",
                column: "recipient_subject_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_transfers_tenant_id_admission_ticket_id",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_transfers_tenant_id_registration_order_id_from_participant_id",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "from_participant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_transfers_tenant_id_registration_order_id_registration_ticket_assignment_id_registration_order_line_id",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_transfers_tenant_id_registration_order_id_to_participant_id",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "to_participant_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_capability",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "capability_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_open",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "open_admission_ticket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_operation",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "offer_operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_transfer_delivery_intents_outbox",
                table: "ie_admission_transfer_delivery_intents",
                column: "outbox_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_transfer_delivery_intents_transfer",
                table: "ie_admission_transfer_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_transfer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_transfer_policies_tenant_id_ticket_catalog_version_id",
                table: "ie_ticket_transfer_policies",
                columns: new[] { "tenant_id", "ticket_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ux_ticket_transfer_policies_ticket_type",
                table: "ie_ticket_transfer_policies",
                columns: new[] { "tenant_id", "event_ticket_type_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_admission_transfer_delivery_intents");

            migrationBuilder.DropTable(
                name: "ie_ticket_transfer_policies");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_transfers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_admission_tickets_transfer_hops",
                table: "ie_admission_tickets");

            migrationBuilder.DropColumn(
                name: "holder_subject_user_id",
                table: "ie_admission_tickets");

            migrationBuilder.DropColumn(
                name: "transfer_hop_count",
                table: "ie_admission_tickets");

            migrationBuilder.AlterColumn<byte[]>(
                name: "lookup_digest",
                table: "ie_admission_recovery_capabilities",
                type: "BLOB",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldFixedLength: true,
                oldMaxLength: 44);

            migrationBuilder.AlterColumn<byte[]>(
                name: "locator_digest",
                table: "ie_admission_recovery_capabilities",
                type: "BLOB",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldFixedLength: true,
                oldMaxLength: 44);
        }
    }
}
