using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionCheckInPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ticket_type_entitlements_tenant_id_ticket_type_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements");

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements",
                type: "uuid",
                nullable: false,
                computedColumnSql: "COALESCE(event_session_id, event_day_id, target_event_id)",
                stored: true);

            migrationBuilder.CreateTable(
                name: "admission_recovery_request_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_identity = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    protection_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_recovery_request_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_recovery_request_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_recovery_request_intents_version", "protection_version > 0");
                    table.ForeignKey(
                        name: "fk_admission_recovery_request_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_targets",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_type_id = table.Column<int>(type: "integer", nullable: false),
                    admission_operational_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_targets", x => x.id);
                    table.UniqueConstraint("ak_admission_targets_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_admission_targets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_targets_operational_status", "admission_operational_status_id IN (1, 2)");
                    table.CheckConstraint("ck_admission_targets_scope_shape", "(admission_target_type_id = 1 AND event_day_id IS NULL AND event_session_id IS NULL AND scope_id = event_id) OR (admission_target_type_id = 2 AND event_day_id IS NOT NULL AND event_session_id IS NULL AND scope_id = event_day_id) OR (admission_target_type_id = 3 AND event_day_id IS NULL AND event_session_id IS NOT NULL AND scope_id = event_session_id)");
                    table.ForeignKey(
                        name: "fk_admission_targets_event_days_tenant_id_event_id_event_day_id",
                        columns: x => new { x.tenant_id, x.event_id, x.event_day_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_targets_event_sessions_tenant_id_event_id_event_s",
                        columns: x => new { x.tenant_id, x.event_id, x.event_session_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_targets_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_targets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_ticket_credential_statuses",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_ticket_credential_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admission_ticket_statuses",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_ticket_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admission_ticket_transition_reasons",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_ticket_transition_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admission_check_in_policies",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opens_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closes_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    maximum_entries = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_check_in_policies", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_policies_maximum_entries", "maximum_entries > 0");
                    table.CheckConstraint("ck_admission_check_in_policies_window", "closes_at_utc > opens_at_utc");
                    table.ForeignKey(
                        name: "fk_admission_check_in_policies_admission_targets_tenant_id_adm",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_scanner_capabilities",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lookup_key_version = table.Column<int>(type: "integer", nullable: false),
                    lookup_digest = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    device_label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    actions = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issued_by_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_scanner_capabilities", x => x.id);
                    table.UniqueConstraint("ak_admission_scanner_capabilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_scanner_capabilities_expiry", "expires_at > issued_at");
                    table.CheckConstraint("ck_admission_scanner_capabilities_key_version", "lookup_key_version > 0");
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_actors_issued_by_actor_id",
                        column: x => x.issued_by_actor_id,
                        principalSchema: "islamu_event",
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_actors_revoked_by_actor_id",
                        column: x => x.revoked_by_actor_id,
                        principalSchema: "islamu_event",
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_admission_targets_tenant_id_",
                        columns: x => new { x.tenant_id, x.event_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_tickets",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_catalog_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    admission_ticket_status_id = table.Column<int>(type: "integer", nullable: false),
                    last_transition_reason_id = table.Column<int>(type: "integer", nullable: false),
                    last_transition_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_tickets", x => x.id);
                    table.UniqueConstraint("ak_admission_tickets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_admission_tickets_admission_ticket_statuses_admission_ticke",
                        column: x => x.admission_ticket_status_id,
                        principalSchema: "islamu_event",
                        principalTable: "admission_ticket_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_admission_ticket_transition_reasons_last_",
                        column: x => x.last_transition_reason_id,
                        principalSchema: "islamu_event",
                        principalTable: "admission_ticket_transition_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_event_ticket_catalog_versions_tenant_id_t",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_event_ticket_types_tenant_id_event_ticket",
                        columns: x => new { x.tenant_id, x.event_ticket_type_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_registration_order_lines_tenant_id_regist",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_order_line_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_order_lines",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_registration_orders_tenant_id_event_id_re",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_registration_participants_tenant_id_regis",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.participant_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_registration_ticket_assignments_tenant_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_tickets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_check_in_events",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    admission_check_in_action_id = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scanner_capability_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admission_check_in_undo_reason_code_id = table.Column<int>(type: "integer", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    compensated_check_in_event_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_check_in_events", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_events_tenant_id_admission_ticket_id_adm", x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.id });
                    table.UniqueConstraint("ak_admission_check_in_events_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_events_action", "admission_check_in_action_id IN (1, 2)");
                    table.CheckConstraint("ck_admission_check_in_events_authority", "(actor_id IS NOT NULL AND scanner_capability_id IS NULL) OR (actor_id IS NULL AND scanner_capability_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_check_in_events_fact_shape", "(admission_check_in_action_id = 1 AND admission_check_in_undo_reason_code_id IS NULL AND compensated_check_in_event_id IS NULL) OR (admission_check_in_action_id = 2 AND admission_check_in_undo_reason_code_id IN (1, 2, 3, 4) AND compensated_check_in_event_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_check_in_events_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_check_in_events_tenant_",
                        columns: x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.compensated_check_in_event_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_check_in_events",
                        principalColumns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_scanner_capabilities_te",
                        columns: x => new { x.tenant_id, x.scanner_capability_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_scanner_capabilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_targets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_tickets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_delivery_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finalization_effect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_credential = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    protection_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    routed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handoff_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handoff_receipt_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_delivery_intents_handoff_receipt", "(handoff_completed_at IS NULL AND handoff_receipt_id IS NULL) OR (handoff_completed_at IS NOT NULL AND handoff_receipt_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_delivery_intents_protection_version", "protection_version > 0");
                    table.ForeignKey(
                        name: "fk_admission_delivery_intents_admission_tickets_tenant_id_admi",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_delivery_intents_registration_finalization_effect",
                        columns: x => new { x.tenant_id, x.finalization_effect_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_finalization_effects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_delivery_intents_registration_ticket_assignments_",
                        columns: x => new { x.tenant_id, x.registration_ticket_assignment_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_delivery_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_recovery_capabilities",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovery_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    capability_version = table.Column<int>(type: "integer", nullable: false),
                    lookup_key_version = table.Column<int>(type: "integer", nullable: false),
                    lookup_digest = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    locator_digest = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    active_uniqueness_slot = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_recovery_capabilities", x => x.id);
                    table.UniqueConstraint("ak_admission_recovery_capabilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_recovery_capabilities_lifecycle", "(consumed_at IS NULL OR rotated_at IS NULL) AND ((consumed_at IS NULL AND rotated_at IS NULL AND active_uniqueness_slot = 0) OR ((consumed_at IS NOT NULL OR rotated_at IS NOT NULL) AND active_uniqueness_slot = capability_version))");
                    table.CheckConstraint("ck_admission_recovery_capabilities_versions", "capability_version > 0 AND lookup_key_version > 0");
                    table.ForeignKey(
                        name: "fk_admission_recovery_capabilities_admission_tickets_tenant_id",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_recovery_capabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_recovery_delivery_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovery_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    capability_version = table.Column<int>(type: "integer", nullable: false),
                    protected_material = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    protection_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    routed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handoff_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handoff_receipt_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_recovery_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_recovery_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_recovery_delivery_intents_handoff", "(handoff_completed_at IS NULL AND handoff_receipt_id IS NULL) OR (handoff_completed_at IS NOT NULL AND handoff_receipt_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_recovery_delivery_intents_versions", "capability_version > 0 AND protection_version > 0");
                    table.ForeignKey(
                        name: "fk_admission_recovery_delivery_intents_admission_tickets_tenan",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_recovery_delivery_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_ticket_credentials",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_version = table.Column<int>(type: "integer", nullable: false),
                    lookup_key_version = table.Column<int>(type: "integer", nullable: false),
                    lookup_digest = table.Column<string>(type: "character(44)", fixedLength: true, maxLength: 44, nullable: false),
                    admission_ticket_credential_status_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    active_uniqueness_slot = table.Column<int>(type: "integer", nullable: false, computedColumnSql: "CASE WHEN admission_ticket_credential_status_id = 1 THEN 0 ELSE credential_version END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_ticket_credentials", x => x.id);
                    table.UniqueConstraint("ak_admission_ticket_credentials_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_ticket_credentials_versions", "credential_version > 0 AND lookup_key_version > 0");
                    table.ForeignKey(
                        name: "fk_admission_ticket_credentials_admission_ticket_credential_st",
                        column: x => x.admission_ticket_credential_status_id,
                        principalSchema: "islamu_event",
                        principalTable: "admission_ticket_credential_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_ticket_credentials_admission_tickets_tenant_id_ad",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_check_in_states",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active_check_in_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_count = table.Column<int>(type: "integer", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_check_in_states", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_states_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_states_counts", "entry_count >= 0 AND last_sequence >= 0");
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_admission_check_in_events_tenant_",
                        columns: x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.active_check_in_event_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_check_in_events",
                        principalColumns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_admission_targets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_admission_tickets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ticket_type_entitlements_canonical_scope",
                schema: "islamu_event",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "ticket_type_id", "target_event_id", "entitlement_scope_type_id", "scope_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LocationPii_CoordinateShape",
                schema: "islamu_event",
                table: "location_pii",
                sql: "(latitude IS NULL AND longitude IS NULL)\nOR (latitude IS NOT NULL AND longitude IS NOT NULL\n    AND latitude BETWEEN -90 AND 90\n    AND longitude BETWEEN -180 AND 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventTicketType_MoneyNonnegative",
                schema: "islamu_event",
                table: "event_ticket_types",
                sql: "(fixed_price_minor IS NULL OR fixed_price_minor >= 0)\nAND (minimum_price_minor IS NULL OR minimum_price_minor >= 0)\nAND (suggested_price_minor IS NULL OR suggested_price_minor >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalDateRange",
                schema: "islamu_event",
                table: "event_sessions",
                sql: "local_end_date IS NULL OR local_start_date IS NULL OR local_end_date >= local_start_date");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAgendaItem_LocalDateRange",
                schema: "islamu_event",
                table: "event_agenda_items",
                sql: "local_end_date >= local_start_date");

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_events_tenant_id_admission_target_id",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_events_tenant_id_admission_ticket_id_adm",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "compensated_check_in_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_events_tenant_id_scanner_capability_id",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "scanner_capability_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_check_in_events_sequence",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_check_in_policies_target",
                schema: "islamu_event",
                table: "admission_check_in_policies",
                columns: new[] { "tenant_id", "admission_target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_states_tenant_id_admission_target_id",
                schema: "islamu_event",
                table: "admission_check_in_states",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_states_tenant_id_admission_ticket_id_adm",
                schema: "islamu_event",
                table: "admission_check_in_states",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "active_check_in_event_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_check_in_states_ticket_target",
                schema: "islamu_event",
                table: "admission_check_in_states",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_delivery_intents_pending",
                schema: "islamu_event",
                table: "admission_delivery_intents",
                columns: new[] { "handoff_completed_at", "routed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_delivery_intents_tenant_id_admission_ticket_id",
                schema: "islamu_event",
                table: "admission_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_delivery_intents_tenant_id_registration_ticket_as",
                schema: "islamu_event",
                table: "admission_delivery_intents",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_delivery_intents_assignment",
                schema: "islamu_event",
                table: "admission_delivery_intents",
                columns: new[] { "tenant_id", "finalization_effect_id", "registration_ticket_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_capabilities_expiry",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                columns: new[] { "expires_at", "consumed_at", "rotated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_capabilities_request",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                columns: new[] { "tenant_id", "recovery_request_id", "purpose" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_active",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                columns: new[] { "tenant_id", "admission_ticket_id", "purpose", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_generation",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                columns: new[] { "tenant_id", "admission_ticket_id", "purpose", "capability_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_locator",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "locator_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_delivery_intents_pending",
                schema: "islamu_event",
                table: "admission_recovery_delivery_intents",
                columns: new[] { "handoff_completed_at", "routed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_delivery_intents_tenant_id_admission_tic",
                schema: "islamu_event",
                table: "admission_recovery_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_delivery_intents_generation",
                schema: "islamu_event",
                table: "admission_recovery_delivery_intents",
                columns: new[] { "tenant_id", "recovery_request_id", "admission_ticket_id", "purpose", "capability_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_request_intents_pending",
                schema: "islamu_event",
                table: "admission_recovery_request_intents",
                columns: new[] { "processed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_issued_by_actor_id",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                column: "issued_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_revoked_by_actor_id",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                column: "revoked_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_target",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_tenant_id_event_id_admission",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "event_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_scanner_capabilities_digest",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_scanner_capabilities_issue_request",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "issue_request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_targets_tenant_id_event_id_event_day_id",
                schema: "islamu_event",
                table: "admission_targets",
                columns: new[] { "tenant_id", "event_id", "event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_targets_tenant_id_event_id_event_session_id",
                schema: "islamu_event",
                table: "admission_targets",
                columns: new[] { "tenant_id", "event_id", "event_session_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_targets_scope",
                schema: "islamu_event",
                table: "admission_targets",
                columns: new[] { "tenant_id", "event_id", "admission_target_type_id", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_credential_statuses_master_code",
                schema: "islamu_event",
                table: "admission_ticket_credential_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_credentials_admission_ticket_credential_st",
                schema: "islamu_event",
                table: "admission_ticket_credentials",
                column: "admission_ticket_credential_status_id");

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_credentials_active",
                schema: "islamu_event",
                table: "admission_ticket_credentials",
                columns: new[] { "tenant_id", "admission_ticket_id", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_credentials_digest",
                schema: "islamu_event",
                table: "admission_ticket_credentials",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_ticket_credentials_version",
                schema: "islamu_event",
                table: "admission_ticket_credentials",
                columns: new[] { "tenant_id", "admission_ticket_id", "credential_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_statuses_master_code",
                schema: "islamu_event",
                table: "admission_ticket_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_ticket_transition_reasons_master_code",
                schema: "islamu_event",
                table: "admission_ticket_transition_reasons",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_admission_ticket_status_id",
                schema: "islamu_event",
                table: "admission_tickets",
                column: "admission_ticket_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_last_transition_reason_id",
                schema: "islamu_event",
                table: "admission_tickets",
                column: "last_transition_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_tenant_id_event_id_registration_order_id",
                schema: "islamu_event",
                table: "admission_tickets",
                columns: new[] { "tenant_id", "event_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_tenant_id_event_ticket_type_id",
                schema: "islamu_event",
                table: "admission_tickets",
                columns: new[] { "tenant_id", "event_ticket_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_tenant_id_registration_order_id_participa",
                schema: "islamu_event",
                table: "admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "participant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_tenant_id_registration_order_id_registrat",
                schema: "islamu_event",
                table: "admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_tenant_id_registration_order_id_registrat1",
                schema: "islamu_event",
                table: "admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_tickets_tenant_id_ticket_catalog_version_id",
                schema: "islamu_event",
                table: "admission_tickets",
                columns: new[] { "tenant_id", "ticket_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_tickets_assignment",
                schema: "islamu_event",
                table: "admission_tickets",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_check_in_policies",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_check_in_states",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_delivery_intents",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_recovery_capabilities",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_recovery_delivery_intents",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_recovery_request_intents",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_ticket_credentials",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_check_in_events",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_ticket_credential_statuses",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_scanner_capabilities",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_tickets",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_targets",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_ticket_statuses",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_ticket_transition_reasons",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ux_ticket_type_entitlements_canonical_scope",
                schema: "islamu_event",
                table: "ticket_type_entitlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LocationPii_CoordinateShape",
                schema: "islamu_event",
                table: "location_pii");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventTicketType_MoneyNonnegative",
                schema: "islamu_event",
                table: "event_ticket_types");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalDateRange",
                schema: "islamu_event",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAgendaItem_LocalDateRange",
                schema: "islamu_event",
                table: "event_agenda_items");

            migrationBuilder.DropColumn(
                name: "scope_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_type_entitlements_tenant_id_ticket_type_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "ticket_type_id" });
        }
    }
}
