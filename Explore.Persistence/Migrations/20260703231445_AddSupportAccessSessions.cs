// ABOUTME: EF Core migration adding support-access session and audit persistence tables.
// ABOUTME: Creates lookup, session, audit, FK, check constraint, and active-session index schema.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportAccessSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "support_access_audit_event_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_lifecycle_event = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_access_audit_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_access_end_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_access_end_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_access_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    allows_writes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_access_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_access_session_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_terminal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_access_session_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "support_access_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    mode_id = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ticket_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_reason_id = table.Column<int>(type: "integer", nullable: true),
                    end_reason_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_access_sessions", x => x.id);
                    table.CheckConstraint("ck_support_access_sessions_end_after_start", "ended_at_utc IS NULL OR ended_at_utc >= started_at_utc");
                    table.CheckConstraint("ck_support_access_sessions_terminal_reason", "(end_reason_id IS NULL) = (ended_at_utc IS NULL)");
                    table.CheckConstraint("ck_support_access_sessions_timebox", "expires_at_utc > started_at_utc");
                    table.ForeignKey(
                        name: "fk_support_access_sessions_support_access_end_reasons_end_reas",
                        column: x => x.end_reason_id,
                        principalTable: "support_access_end_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_sessions_support_access_modes_mode_id",
                        column: x => x.mode_id,
                        principalTable: "support_access_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_sessions_support_access_session_statuses_sta",
                        column: x => x.status_id,
                        principalTable: "support_access_session_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_sessions_tenant_users_target_tenant_user_id",
                        column: x => x.target_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_sessions_tenants_target_tenant_id",
                        column: x => x.target_tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_sessions_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_sessions_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "support_access_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    support_access_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_type_id = table.Column<int>(type: "integer", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    route_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    request_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resource_kind = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resource_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sanitized_metadata_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_access_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_support_access_audit_events_support_access_audit_event_type",
                        column: x => x.event_type_id,
                        principalTable: "support_access_audit_event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_audit_events_support_access_sessions_support",
                        column: x => x.support_access_session_id,
                        principalTable: "support_access_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_audit_events_tenant_users_target_tenant_user",
                        column: x => x.target_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_audit_events_tenants_target_tenant_id",
                        column: x => x.target_tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_support_access_audit_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_support_access_audit_event_types_master_code",
                table: "support_access_audit_event_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_support_access_audit_events_actor_occurred",
                table: "support_access_audit_events",
                columns: new[] { "actor_user_id", "occurred_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_support_access_audit_events_event_type_id",
                table: "support_access_audit_events",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_access_audit_events_session_occurred",
                table: "support_access_audit_events",
                columns: new[] { "support_access_session_id", "occurred_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_support_access_audit_events_target_tenant_user_id",
                table: "support_access_audit_events",
                column: "target_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_access_audit_events_tenant_occurred",
                table: "support_access_audit_events",
                columns: new[] { "target_tenant_id", "occurred_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_support_access_end_reasons_master_code",
                table: "support_access_end_reasons",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_support_access_modes_master_code",
                table: "support_access_modes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_support_access_session_statuses_master_code",
                table: "support_access_session_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_actor_status_expires",
                table: "support_access_sessions",
                columns: new[] { "actor_user_id", "status_id", "expires_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_approved_by_user_id",
                table: "support_access_sessions",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_end_reason_id",
                table: "support_access_sessions",
                column: "end_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_id_actor_status",
                table: "support_access_sessions",
                columns: new[] { "id", "actor_user_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_mode_id",
                table: "support_access_sessions",
                column: "mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_status_id",
                table: "support_access_sessions",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_target_tenant_started",
                table: "support_access_sessions",
                columns: new[] { "target_tenant_id", "started_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_support_access_sessions_target_tenant_user_id",
                table: "support_access_sessions",
                column: "target_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_support_access_sessions_active_actor",
                table: "support_access_sessions",
                column: "actor_user_id",
                unique: true,
                filter: "status_id = 2 AND ended_at_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_access_audit_events");

            migrationBuilder.DropTable(
                name: "support_access_audit_event_types");

            migrationBuilder.DropTable(
                name: "support_access_sessions");

            migrationBuilder.DropTable(
                name: "support_access_end_reasons");

            migrationBuilder.DropTable(
                name: "support_access_modes");

            migrationBuilder.DropTable(
                name: "support_access_session_statuses");
        }
    }
}
