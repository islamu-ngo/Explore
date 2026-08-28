using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
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
                type: "char(36)",
                nullable: true)
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "transfer_hop_count",
                table: "ie_admission_tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "lookup_digest",
                table: "ie_admission_recovery_capabilities",
                type: "char(44)",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "binary(32)",
                oldFixedLength: true,
                oldMaxLength: 32)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "locator_digest",
                table: "ie_admission_recovery_capabilities",
                type: "char(44)",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "binary(32)",
                oldFixedLength: true,
                oldMaxLength: 32)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    open_admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_line_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    from_participant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    to_participant_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recipient_subject_user_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    offer_operation_key = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    capability_digest = table.Column<string>(type: "char(44)", fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    transfer_hop = table.Column<int>(type: "int", nullable: false),
                    credential_generation = table.Column<int>(type: "int", nullable: false),
                    accepted_credential_generation = table.Column<int>(type: "int", nullable: true),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    offered_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    capability_consumed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    accepted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expired_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("pk_ie_admission_ticket_transfers", x => x.id);
                    table.UniqueConstraint("ak_admission_ticket_transfers_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_ticket_transfers_positive", "transfer_hop > 0 AND credential_generation > 0 AND (accepted_credential_generation IS NULL OR accepted_credential_generation = credential_generation + 1)");
                    table.CheckConstraint("ck_admission_ticket_transfers_status", "status_id BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_admission_ticket_transfers_terminal_facts", "(status_id = 1 AND accepted_at IS NULL AND cancelled_at IS NULL AND expired_at IS NULL AND capability_consumed_at IS NULL AND accepted_credential_generation IS NULL AND to_participant_id IS NULL AND recipient_subject_user_id IS NULL) OR (status_id = 2 AND accepted_at IS NOT NULL AND capability_consumed_at IS NOT NULL AND accepted_credential_generation IS NOT NULL AND to_participant_id IS NOT NULL AND recipient_subject_user_id IS NOT NULL AND cancelled_at IS NULL AND expired_at IS NULL) OR (status_id = 3 AND cancelled_at IS NOT NULL AND accepted_at IS NULL AND expired_at IS NULL) OR (status_id = 4 AND expired_at IS NOT NULL AND accepted_at IS NULL AND cancelled_at IS NULL)");
                    table.ForeignKey(
                        name: "FK_ie_admission_ticket_transfers_ie_admission_tickets_t_47476C0A",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_ticket_transfers_ie_registration_partic_1CD63242",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.from_participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_ticket_transfers_ie_registration_partic_70EBBCF1",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.to_participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_ticket_transfers_ie_registration_ticket_B80DAF79",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_ticket_transfers_ie_users_recipient_sub_E84188AB",
                        column: x => x.recipient_subject_user_id,
                        principalTable: "ie_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_transfers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_ticket_transfer_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    ticket_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_ticket_type_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    maximum_hops = table.Column<int>(type: "int", nullable: false),
                    offer_lifetime_minutes = table.Column<int>(type: "int", nullable: false),
                    cutoff_minutes_before_event = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_ie_ticket_transfer_policies", x => x.id);
                    table.UniqueConstraint("ak_ticket_transfer_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_ticket_transfer_policies_bounds", "maximum_hops BETWEEN 1 AND 100 AND offer_lifetime_minutes BETWEEN 5 AND 43200 AND cutoff_minutes_before_event BETWEEN 0 AND 525600");
                    table.ForeignKey(
                        name: "FK_ie_ticket_transfer_policies_ie_event_ticket_catalog__A823FCF2",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalTable: "ie_event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_ticket_transfer_policies_ie_event_ticket_types_te_EDC53082",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_transfer_delivery_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_transfer_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    outbox_message_id = table.Column<Guid>(type: "char(36)", nullable: false)
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
                    table.PrimaryKey("pk_ie_admission_transfer_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_transfer_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_admission_transfer_delivery_intents_ie_admission__BFADFDB9",
                        columns: x => new { x.tenant_id, x.admission_ticket_transfer_id },
                        principalTable: "ie_admission_ticket_transfers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_transfer_delivery_intents_ie_outbox_mes_EEB0AC47",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "IX_ie_admission_ticket_transfers_tenant_id_open_admissi_F91A7C5D",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "open_admission_ticket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_ticket_transfers_tenant_id_registration_19E2353B",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "to_participant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_ticket_transfers_tenant_id_registration_C02BB593",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "from_participant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_ticket_transfers_tenant_id_registration_E38D89E8",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_capability",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "capability_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_transfers_operation",
                table: "ie_admission_ticket_transfers",
                columns: new[] { "tenant_id", "offer_operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_transfer_delivery_intents_tenant_id_adm_589612F1",
                table: "ie_admission_transfer_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_transfer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_transfer_delivery_intents_outbox",
                table: "ie_admission_transfer_delivery_intents",
                column: "outbox_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_ticket_transfer_policies_tenant_id_ticket_catalog_66833FA1",
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
                type: "binary(32)",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(44)",
                oldFixedLength: true,
                oldMaxLength: 44)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<byte[]>(
                name: "locator_digest",
                table: "ie_admission_recovery_capabilities",
                type: "binary(32)",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(44)",
                oldFixedLength: true,
                oldMaxLength: 44)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
