using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookAdministrativeAuditLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_audit_actions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_audit_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_audit_outcomes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_audit_outcomes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_audit_principal_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_audit_principal_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_audit_scope_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_audit_scope_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_audit_target_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_audit_target_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    principal_kind_id = table.Column<int>(type: "integer", nullable: false),
                    principal_reference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    effective_scope_kind_id = table.Column<int>(type: "integer", nullable: false),
                    effective_scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_id = table.Column<int>(type: "integer", nullable: false),
                    target_kind_id = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    safe_before_json = table.Column<string>(type: "jsonb", nullable: true),
                    safe_after_json = table.Column<string>(type: "jsonb", nullable: true),
                    configuration_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reason_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    outcome_id = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "statement_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_audit_events", x => x.id);
                    table.UniqueConstraint("ak_webhook_audit_events_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_webhook_audit_events_safe_after_object", "safe_after_json IS NULL OR jsonb_typeof(safe_after_json) = 'object'");
                    table.CheckConstraint("ck_webhook_audit_events_safe_before_object", "safe_before_json IS NULL OR jsonb_typeof(safe_before_json) = 'object'");
                    table.CheckConstraint("ck_webhook_audit_events_tenant_scope", "effective_scope_kind_id <> 1 OR effective_scope_id = tenant_id");
                    table.ForeignKey(
                        name: "fk_webhook_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_audit_events_webhook_audit_actions_action_id",
                        column: x => x.action_id,
                        principalTable: "webhook_audit_actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_audit_events_webhook_audit_outcomes_outcome_id",
                        column: x => x.outcome_id,
                        principalTable: "webhook_audit_outcomes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_audit_events_webhook_audit_principal_kinds_principa",
                        column: x => x.principal_kind_id,
                        principalTable: "webhook_audit_principal_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_audit_events_webhook_audit_scope_kinds_effective_sc",
                        column: x => x.effective_scope_kind_id,
                        principalTable: "webhook_audit_scope_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_audit_events_webhook_audit_target_kinds_target_kind",
                        column: x => x.target_kind_id,
                        principalTable: "webhook_audit_target_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_audit_actions_master_code",
                table: "webhook_audit_actions",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_action_id",
                table: "webhook_audit_events",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_effective_scope_kind_id",
                table: "webhook_audit_events",
                column: "effective_scope_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_outcome_id",
                table: "webhook_audit_events",
                column: "outcome_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_principal_kind_id",
                table: "webhook_audit_events",
                column: "principal_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_target_kind_id",
                table: "webhook_audit_events",
                column: "target_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_tenant_correlation",
                table: "webhook_audit_events",
                columns: new[] { "tenant_id", "correlation_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_tenant_occurred",
                table: "webhook_audit_events",
                columns: new[] { "tenant_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_tenant_target_occurred",
                table: "webhook_audit_events",
                columns: new[] { "tenant_id", "target_kind_id", "target_id", "occurred_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_audit_outcomes_master_code",
                table: "webhook_audit_outcomes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_audit_principal_kinds_master_code",
                table: "webhook_audit_principal_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_audit_scope_kinds_master_code",
                table: "webhook_audit_scope_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_audit_target_kinds_master_code",
                table: "webhook_audit_target_kinds",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_audit_events");

            migrationBuilder.DropTable(
                name: "webhook_audit_actions");

            migrationBuilder.DropTable(
                name: "webhook_audit_outcomes");

            migrationBuilder.DropTable(
                name: "webhook_audit_principal_kinds");

            migrationBuilder.DropTable(
                name: "webhook_audit_scope_kinds");

            migrationBuilder.DropTable(
                name: "webhook_audit_target_kinds");
        }
    }
}
