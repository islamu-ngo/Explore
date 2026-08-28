using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionTicketTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "holder_subject_user_id",
                schema: "islamu_event",
                table: "admission_tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "transfer_hop_count",
                schema: "islamu_event",
                table: "admission_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "lookup_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "character(44)",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldFixedLength: true,
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "locator_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "character(44)",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldFixedLength: true,
                oldMaxLength: 32);

            migrationBuilder.CreateTable(
                name: "admission_ticket_transfers",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    open_admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_participant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_subject_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    offer_operation_key = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_digest = table.Column<string>(type: "character(44)", fixedLength: true, maxLength: 44, nullable: false),
                    transfer_hop = table.Column<int>(type: "integer", nullable: false),
                    credential_generation = table.Column<int>(type: "integer", nullable: false),
                    accepted_credential_generation = table.Column<int>(type: "integer", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    offered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    capability_consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_ticket_transfers", x => x.id);
                    table.UniqueConstraint("ak_admission_ticket_transfers_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_ticket_transfers_positive", "transfer_hop > 0 AND credential_generation > 0 AND (accepted_credential_generation IS NULL OR accepted_credential_generation = credential_generation + 1)");
                    table.CheckConstraint("ck_admission_ticket_transfers_status", "status_id BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_admission_ticket_transfers_terminal_facts", "(status_id = 1 AND accepted_at IS NULL AND cancelled_at IS NULL AND expired_at IS NULL AND capability_consumed_at IS NULL AND accepted_credential_generation IS NULL AND to_participant_id IS NULL AND recipient_subject_user_id IS NULL) OR (status_id = 2 AND accepted_at IS NOT NULL AND capability_consumed_at IS NOT NULL AND accepted_credential_generation IS NOT NULL AND to_participant_id IS NOT NULL AND recipient_subject_user_id IS NOT NULL AND cancelled_at IS NULL AND expired_at IS NULL) OR (status_id = 3 AND cancelled_at IS NOT NULL AND accepted_at IS NULL AND expired_at IS NULL) OR (status_id = 4 AND expired_at IS NOT NULL AND accepted_at IS NULL AND cancelled_at IS NULL)");
                    table.ForeignKey(
                        name: "fk_admission_ticket_transfers_admission_tickets_tenant_id_admi",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_ticket_transfers_registration_participants_tenant",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.from_participant_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_ticket_transfers_registration_participants_tenant1",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.to_participant_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_ticket_transfers_registration_ticket_assignments_",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_ticket_transfers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_ticket_transfers_users_recipient_subject_user_id",
                        column: x => x.recipient_subject_user_id,
                        principalSchema: "islamu_event",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ticket_transfer_policies",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    maximum_hops = table.Column<int>(type: "integer", nullable: false),
                    offer_lifetime_minutes = table.Column<int>(type: "integer", nullable: false),
                    cutoff_minutes_before_event = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_transfer_policies", x => x.id);
                    table.UniqueConstraint("ak_ticket_transfer_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_ticket_transfer_policies_bounds", "maximum_hops BETWEEN 1 AND 100 AND offer_lifetime_minutes BETWEEN 5 AND 43200 AND cutoff_minutes_before_event BETWEEN 0 AND 525600");
                    table.ForeignKey(
                        name: "fk_ticket_transfer_policies_event_ticket_catalog_versions_tena",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_transfer_policies_event_ticket_types_tenant_id_event",
                        columns: x => new { x.tenant_id, x.event_ticket_type_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_transfer_policies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_transfer_delivery_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_transfer_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_transfer_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_admission_transfer_delivery_intents_admission_ticket_transf",
                        columns: x => new { x.tenant_id, x.admission_ticket_transfer_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_ticket_transfers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_transfer_delivery_intents_outbox_messages_outbox_",
                        column: x => x.outbox_message_id,
                        principalSchema: "islamu_event",
                        principalTable: "outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_transfer_delivery_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_admission_tickets_transfer_hops",
                schema: "islamu_event",
                table: "admission_tickets",
                sql: "transfer_hop_count >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_transfers_recipient_subject_user_id",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                column: "recipient_subject_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_transfers_tenant_id_admission_ticket_id",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_transfers_tenant_id_registration_order_id_",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "from_participant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_transfers_tenant_id_registration_order_id_1",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "to_participant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_transfers_tenant_id_registration_order_id_2",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_capability",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                columns: new[] { "tenant_id", "capability_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_open",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                columns: new[] { "tenant_id", "open_admission_ticket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_operation",
                schema: "islamu_event",
                table: "admission_ticket_transfers",
                columns: new[] { "tenant_id", "offer_operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_transfer_delivery_intents_outbox",
                schema: "islamu_event",
                table: "admission_transfer_delivery_intents",
                column: "outbox_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_transfer_delivery_intents_transfer",
                schema: "islamu_event",
                table: "admission_transfer_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_transfer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_transfer_policies_tenant_id_ticket_catalog_version_id",
                schema: "islamu_event",
                table: "ticket_transfer_policies",
                columns: new[] { "tenant_id", "ticket_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ux_ticket_transfer_policies_ticket_type",
                schema: "islamu_event",
                table: "ticket_transfer_policies",
                columns: new[] { "tenant_id", "event_ticket_type_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_transfer_delivery_intents",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "ticket_transfer_policies",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_ticket_transfers",
                schema: "islamu_event");

            migrationBuilder.DropCheckConstraint(
                name: "ck_admission_tickets_transfer_hops",
                schema: "islamu_event",
                table: "admission_tickets");

            migrationBuilder.DropColumn(
                name: "holder_subject_user_id",
                schema: "islamu_event",
                table: "admission_tickets");

            migrationBuilder.DropColumn(
                name: "transfer_hop_count",
                schema: "islamu_event",
                table: "admission_tickets");

            migrationBuilder.AlterColumn<byte[]>(
                name: "lookup_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "bytea",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(44)",
                oldFixedLength: true,
                oldMaxLength: 44);

            migrationBuilder.AlterColumn<byte[]>(
                name: "locator_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "bytea",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(44)",
                oldFixedLength: true,
                oldMaxLength: 44);
        }
    }
}
