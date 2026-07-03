// ABOUTME: EF Core migration adding event-reporting persistence tables.
// ABOUTME: Creates tenant-safe report, evidence, case, signal, decision, and provider-link schema.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_storage_objects_tenant_id_id",
                table: "storage_objects",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "event_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporter_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporter_kind = table.Column<int>(type: "integer", nullable: false),
                    source_kind = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subcategory_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    severity_hint = table.Column<int>(type: "integer", nullable: true),
                    duplicate_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reporter_contact_consent = table.Column<bool>(type: "boolean", nullable: false),
                    reporter_locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    reporter_ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reporter_user_agent_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_reports", x => x.id);
                    table.UniqueConstraint("ak_event_reports_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_reports_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_reports_closed_at_terminal_status", "(closed_at IS NULL AND status NOT IN (4, 5, 6, 8)) OR (closed_at IS NOT NULL AND status IN (4, 5, 6, 8))");
                    table.CheckConstraint("ck_event_reports_priority", "priority BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_event_reports_reason_code_not_blank", "length(btrim(reason_code)) > 0");
                    table.CheckConstraint("ck_event_reports_reporter_ip_hash_not_blank", "reporter_ip_hash IS NULL OR length(btrim(reporter_ip_hash)) > 0");
                    table.CheckConstraint("ck_event_reports_reporter_kind", "reporter_kind BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_event_reports_reporter_locale_not_blank", "reporter_locale IS NULL OR length(btrim(reporter_locale)) > 0");
                    table.CheckConstraint("ck_event_reports_reporter_user_agent_hash_not_blank", "reporter_user_agent_hash IS NULL OR length(btrim(reporter_user_agent_hash)) > 0");
                    table.CheckConstraint("ck_event_reports_severity_hint", "severity_hint IS NULL OR severity_hint BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_event_reports_source_kind", "source_kind BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_event_reports_status", "status BETWEEN 1 AND 8");
                    table.CheckConstraint("ck_event_reports_subcategory_code_not_blank", "subcategory_code IS NULL OR length(btrim(subcategory_code)) > 0");
                    table.ForeignKey(
                        name: "fk_event_reports_actors_tenant_id_reporter_actor_id",
                        columns: x => new { x.tenant_id, x.reporter_actor_id },
                        principalTable: "actors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_reports_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_reports_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_reports_users_reporter_user_id",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_report_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    queue_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    assigned_moderator_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sla_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_report_cases", x => x.id);
                    table.UniqueConstraint("ak_event_report_cases_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.UniqueConstraint("ak_event_report_cases_tenant_id_report_id_id", x => new { x.tenant_id, x.report_id, x.id });
                    table.CheckConstraint("ck_event_report_cases_priority", "priority BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_event_report_cases_queue_code_not_blank", "length(btrim(queue_code)) > 0");
                    table.CheckConstraint("ck_event_report_cases_status", "status BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "fk_event_report_cases_event_reports_tenant_id_report_id",
                        columns: x => new { x.tenant_id, x.report_id },
                        principalTable: "event_reports",
                        principalColumns: new[] { "tenant_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_cases_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_cases_users_assigned_moderator_user_id",
                        column: x => x.assigned_moderator_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_report_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_kind = table.Column<int>(type: "integer", nullable: false),
                    text_body_encrypted = table.Column<string>(type: "text", nullable: true),
                    storage_object_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    classification = table.Column<int>(type: "integer", nullable: false),
                    retention_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_report_evidence", x => x.id);
                    table.CheckConstraint("ck_event_report_evidence_classification", "classification BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_event_report_evidence_content_hash_not_blank", "content_hash IS NULL OR length(btrim(content_hash)) > 0");
                    table.CheckConstraint("ck_event_report_evidence_kind", "evidence_kind BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_event_report_evidence_reporter_text_required", "evidence_kind <> 1 OR (text_body_encrypted IS NOT NULL AND length(btrim(text_body_encrypted)) > 0)");
                    table.ForeignKey(
                        name: "fk_event_report_evidence_event_reports_tenant_id_report_id",
                        columns: x => new { x.tenant_id, x.report_id },
                        principalTable: "event_reports",
                        principalColumns: new[] { "tenant_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_evidence_storage_objects_tenant_id_storage_obj",
                        columns: x => new { x.tenant_id, x.storage_object_id },
                        principalTable: "storage_objects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_evidence_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_report_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    signal_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    policy_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    verdict = table.Column<int>(type: "integer", nullable: false),
                    recommended_action = table.Column<int>(type: "integer", nullable: true),
                    safe_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_signal_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_report_signals", x => x.id);
                    table.CheckConstraint("ck_event_report_signals_correlation_id_not_blank", "length(btrim(correlation_id)) > 0");
                    table.CheckConstraint("ck_event_report_signals_external_signal_id_not_blank", "external_signal_id IS NULL OR length(btrim(external_signal_id)) > 0");
                    table.CheckConstraint("ck_event_report_signals_policy_code_not_blank", "length(btrim(policy_code)) > 0");
                    table.CheckConstraint("ck_event_report_signals_provider", "provider BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_event_report_signals_recommended_action", "recommended_action IS NULL OR recommended_action BETWEEN 0 AND 4");
                    table.CheckConstraint("ck_event_report_signals_safe_summary_not_blank", "safe_summary IS NULL OR length(btrim(safe_summary)) > 0");
                    table.CheckConstraint("ck_event_report_signals_score_range", "score IS NULL OR (score >= 0 AND score <= 1)");
                    table.CheckConstraint("ck_event_report_signals_signal_type_not_blank", "length(btrim(signal_type)) > 0");
                    table.CheckConstraint("ck_event_report_signals_verdict", "verdict BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_event_report_signals_event_reports_tenant_id_event_id_repor",
                        columns: x => new { x.tenant_id, x.event_id, x.report_id },
                        principalTable: "event_reports",
                        principalColumns: new[] { "tenant_id", "event_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_signals_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_signals_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_report_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_kind = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    storage_object_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_report_targets", x => x.id);
                    table.CheckConstraint("ck_event_report_targets_field_path_not_blank", "field_path IS NULL OR length(btrim(field_path)) > 0");
                    table.CheckConstraint("ck_event_report_targets_target_kind", "target_kind BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "fk_event_report_targets_event_reports_tenant_id_report_id",
                        columns: x => new { x.tenant_id, x.report_id },
                        principalTable: "event_reports",
                        principalColumns: new[] { "tenant_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_targets_storage_objects_tenant_id_storage_obje",
                        columns: x => new { x.tenant_id, x.storage_object_id },
                        principalTable: "storage_objects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_targets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_report_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_source = table.Column<int>(type: "integer", nullable: false),
                    decision_kind = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    safe_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    moderator_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_decision_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_report_decisions", x => x.id);
                    table.CheckConstraint("ck_event_report_decisions_external_decision_id_not_blank", "external_decision_id IS NULL OR length(btrim(external_decision_id)) > 0");
                    table.CheckConstraint("ck_event_report_decisions_kind", "decision_kind BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_event_report_decisions_local_moderator_required", "decision_source <> 1 OR moderator_user_id IS NOT NULL");
                    table.CheckConstraint("ck_event_report_decisions_reason_code_not_blank", "length(btrim(reason_code)) > 0");
                    table.CheckConstraint("ck_event_report_decisions_safe_note_not_blank", "safe_note IS NULL OR length(btrim(safe_note)) > 0");
                    table.CheckConstraint("ck_event_report_decisions_source", "decision_source BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_event_report_decisions_event_report_cases_tenant_id_report_",
                        columns: x => new { x.tenant_id, x.report_id, x.case_id },
                        principalTable: "event_report_cases",
                        principalColumns: new[] { "tenant_id", "report_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_decisions_event_reports_tenant_id_report_id",
                        columns: x => new { x.tenant_id, x.report_id },
                        principalTable: "event_reports",
                        principalColumns: new[] { "tenant_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_decisions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_decisions_users_moderator_user_id",
                        column: x => x.moderator_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_report_external_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    provider_case_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_signal_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sync_state = table.Column<int>(type: "integer", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_report_external_links", x => x.id);
                    table.CheckConstraint("ck_event_report_external_links_correlation_id_not_blank", "length(btrim(correlation_id)) > 0");
                    table.CheckConstraint("ck_event_report_external_links_last_error_category_not_blank", "last_error_category IS NULL OR length(btrim(last_error_category)) > 0");
                    table.CheckConstraint("ck_event_report_external_links_provider", "provider BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_event_report_external_links_provider_case_id_not_blank", "provider_case_id IS NULL OR length(btrim(provider_case_id)) > 0");
                    table.CheckConstraint("ck_event_report_external_links_provider_signal_id_not_blank", "provider_signal_id IS NULL OR length(btrim(provider_signal_id)) > 0");
                    table.CheckConstraint("ck_event_report_external_links_provider_url_not_blank", "provider_url IS NULL OR length(btrim(provider_url)) > 0");
                    table.CheckConstraint("ck_event_report_external_links_retry_count_nonnegative", "retry_count >= 0");
                    table.CheckConstraint("ck_event_report_external_links_sync_state", "sync_state BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_event_report_external_links_event_report_cases_tenant_id_re",
                        columns: x => new { x.tenant_id, x.report_id, x.case_id },
                        principalTable: "event_report_cases",
                        principalColumns: new[] { "tenant_id", "report_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_external_links_event_reports_tenant_id_report_",
                        columns: x => new { x.tenant_id, x.report_id },
                        principalTable: "event_reports",
                        principalColumns: new[] { "tenant_id", "id" });
                    table.ForeignKey(
                        name: "fk_event_report_external_links_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_cases_assigned_moderator_user_id",
                table: "event_report_cases",
                column: "assigned_moderator_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_cases_tenant_assignee_status_updated",
                table: "event_report_cases",
                columns: new[] { "tenant_id", "assigned_moderator_user_id", "status", "updated_at" },
                descending: new[] { false, false, false, true },
                filter: "assigned_moderator_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_cases_tenant_queue_status_priority_created",
                table: "event_report_cases",
                columns: new[] { "tenant_id", "queue_code", "status", "priority", "created_at" },
                descending: new[] { false, false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_cases_tenant_sla_due_at",
                table: "event_report_cases",
                columns: new[] { "tenant_id", "sla_due_at" },
                filter: "sla_due_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_decisions_moderator_user_id",
                table: "event_report_decisions",
                column: "moderator_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_decisions_tenant_case_created",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "case_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_decisions_tenant_id_report_id_case_id",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "report_id", "case_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_decisions_tenant_report_created",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "report_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_event_report_decisions_tenant_source_external",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "decision_source", "external_decision_id" },
                unique: true,
                filter: "external_decision_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_evidence_created_by_user_id",
                table: "event_report_evidence",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_evidence_tenant_content_hash",
                table: "event_report_evidence",
                columns: new[] { "tenant_id", "content_hash" },
                filter: "content_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_evidence_tenant_id_storage_object_id",
                table: "event_report_evidence",
                columns: new[] { "tenant_id", "storage_object_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_evidence_tenant_report_kind_created",
                table: "event_report_evidence",
                columns: new[] { "tenant_id", "report_id", "evidence_kind", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_evidence_tenant_retention_until",
                table: "event_report_evidence",
                columns: new[] { "tenant_id", "retention_until" },
                filter: "retention_until IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_external_links_tenant_id_report_id_case_id",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "report_id", "case_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_external_links_tenant_provider_state_created",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "sync_state", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_event_report_external_links_tenant_provider_case",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "provider_case_id" },
                unique: true,
                filter: "provider_case_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_event_report_external_links_tenant_provider_correlation",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "correlation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_event_report_external_links_tenant_provider_signal",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "provider_signal_id" },
                unique: true,
                filter: "provider_signal_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_signals_tenant_event_provider_created",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "event_id", "provider", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_signals_tenant_id_event_id_report_id",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "event_id", "report_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_signals_tenant_report_provider_created",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "report_id", "provider", "created_at" },
                descending: new[] { false, false, false, true },
                filter: "report_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_event_report_signals_tenant_provider_correlation",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "provider", "correlation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_event_report_signals_tenant_provider_external_signal",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "provider", "external_signal_id" },
                unique: true,
                filter: "external_signal_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_targets_tenant_id_storage_object_id",
                table: "event_report_targets",
                columns: new[] { "tenant_id", "storage_object_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_targets_tenant_report_target",
                table: "event_report_targets",
                columns: new[] { "tenant_id", "report_id", "target_kind", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_targets_tenant_target",
                table: "event_report_targets",
                columns: new[] { "tenant_id", "target_kind", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_reporter_user_id",
                table: "event_reports",
                column: "reporter_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_tenant_duplicate_group",
                table: "event_reports",
                columns: new[] { "tenant_id", "duplicate_group_id" },
                filter: "duplicate_group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_tenant_event_status_created",
                table: "event_reports",
                columns: new[] { "tenant_id", "event_id", "status", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_tenant_id_reporter_actor_id",
                table: "event_reports",
                columns: new[] { "tenant_id", "reporter_actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_tenant_priority_status_created",
                table: "event_reports",
                columns: new[] { "tenant_id", "priority", "status", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_reports_tenant_reporter_event_reason_created",
                table: "event_reports",
                columns: new[] { "tenant_id", "reporter_user_id", "event_id", "reason_code", "created_at" },
                descending: new[] { false, false, false, false, true },
                filter: "reporter_user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_report_decisions");

            migrationBuilder.DropTable(
                name: "event_report_evidence");

            migrationBuilder.DropTable(
                name: "event_report_external_links");

            migrationBuilder.DropTable(
                name: "event_report_signals");

            migrationBuilder.DropTable(
                name: "event_report_targets");

            migrationBuilder.DropTable(
                name: "event_report_cases");

            migrationBuilder.DropTable(
                name: "event_reports");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_storage_objects_tenant_id_id",
                table: "storage_objects");
        }
    }
}
