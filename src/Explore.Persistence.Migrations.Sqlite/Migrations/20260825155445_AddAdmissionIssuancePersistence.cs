using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionIssuancePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_credential_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    master_code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_credential_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    master_code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_transition_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    master_code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_transition_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_admission_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    participant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_ticket_type_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    display_reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    admission_ticket_status_id = table.Column<int>(type: "INTEGER", nullable: false),
                    last_transition_reason_id = table.Column<int>(type: "INTEGER", nullable: false),
                    last_transition_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_tickets", x => x.id);
                    table.UniqueConstraint("ak_admission_tickets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_admission_ticket_statuses_admission_ticket_status_id",
                        column: x => x.admission_ticket_status_id,
                        principalTable: "ie_admission_ticket_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_admission_ticket_transition_reasons_last_transition_reason_id",
                        column: x => x.last_transition_reason_id,
                        principalTable: "ie_admission_ticket_transition_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_event_ticket_catalog_versions_tenant_id_ticket_catalog_version_id",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalTable: "ie_event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_event_ticket_types_tenant_id_event_ticket_type_id",
                        columns: x => new { x.tenant_id, x.event_ticket_type_id },
                        principalTable: "ie_event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_registration_order_lines_tenant_id_registration_order_id_registration_order_line_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_order_line_id },
                        principalTable: "ie_registration_order_lines",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_registration_orders_tenant_id_event_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_registration_participants_tenant_id_registration_order_id_participant_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_registration_ticket_assignments_tenant_id_registration_order_id_registration_ticket_assignment_id_registration_order_line_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_admission_delivery_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    finalization_effect_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    protected_credential = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    protection_version = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    routed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    handoff_completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    handoff_receipt_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_delivery_intents_handoff_receipt", "(handoff_completed_at IS NULL AND handoff_receipt_id IS NULL) OR (handoff_completed_at IS NOT NULL AND handoff_receipt_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_delivery_intents_protection_version", "protection_version > 0");
                    table.ForeignKey(
                        name: "fk_ie_admission_delivery_intents_admission_tickets_tenant_id_admission_ticket_id",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_delivery_intents_registration_finalization_effects_tenant_id_finalization_effect_id",
                        columns: x => new { x.tenant_id, x.finalization_effect_id },
                        principalTable: "ie_registration_finalization_effects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_delivery_intents_registration_ticket_assignments_tenant_id_registration_ticket_assignment_id",
                        columns: x => new { x.tenant_id, x.registration_ticket_assignment_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_delivery_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    credential_version = table.Column<int>(type: "INTEGER", nullable: false),
                    lookup_key_version = table.Column<int>(type: "INTEGER", nullable: false),
                    lookup_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 44, nullable: false),
                    admission_ticket_credential_status_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    active_uniqueness_slot = table.Column<int>(type: "INTEGER", nullable: false, computedColumnSql: "CASE WHEN admission_ticket_credential_status_id = 1 THEN 0 ELSE credential_version END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_credentials", x => x.id);
                    table.UniqueConstraint("ak_admission_ticket_credentials_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_ticket_credentials_versions", "credential_version > 0 AND lookup_key_version > 0");
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_credentials_admission_ticket_credential_statuses_admission_ticket_credential_status_id",
                        column: x => x.admission_ticket_credential_status_id,
                        principalTable: "ie_admission_ticket_credential_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_ticket_credentials_ie_admission_tickets_tenant_id_admission_ticket_id",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admission_delivery_intents_pending",
                table: "ie_admission_delivery_intents",
                columns: new[] { "handoff_completed_at", "routed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_delivery_intents_tenant_id_admission_ticket_id",
                table: "ie_admission_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_delivery_intents_tenant_id_registration_ticket_assignment_id",
                table: "ie_admission_delivery_intents",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_delivery_intents_assignment",
                table: "ie_admission_delivery_intents",
                columns: new[] { "tenant_id", "finalization_effect_id", "registration_ticket_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_credential_statuses_master_code",
                table: "ie_admission_ticket_credential_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_credentials_admission_ticket_credential_status_id",
                table: "ie_admission_ticket_credentials",
                column: "admission_ticket_credential_status_id");

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_credentials_active",
                table: "ie_admission_ticket_credentials",
                columns: new[] { "tenant_id", "admission_ticket_id", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_credentials_digest",
                table: "ie_admission_ticket_credentials",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_credentials_version",
                table: "ie_admission_ticket_credentials",
                columns: new[] { "tenant_id", "admission_ticket_id", "credential_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_statuses_master_code",
                table: "ie_admission_ticket_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_transition_reasons_master_code",
                table: "ie_admission_ticket_transition_reasons",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_admission_ticket_status_id",
                table: "ie_admission_tickets",
                column: "admission_ticket_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_last_transition_reason_id",
                table: "ie_admission_tickets",
                column: "last_transition_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_event_id_registration_order_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "event_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_event_ticket_type_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "event_ticket_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_registration_order_id_participant_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "participant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_registration_order_id_registration_order_line_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_registration_order_id_registration_ticket_assignment_id_registration_order_line_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_ticket_catalog_version_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "ticket_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_tickets_assignment",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_admission_delivery_intents");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_credentials");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_credential_statuses");

            migrationBuilder.DropTable(
                name: "ie_admission_tickets");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_statuses");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_transition_reasons");
        }
    }
}
