// ABOUTME: Adds durable decision execution claims and exact moderation-receipt uniqueness.
// ABOUTME: Backfills every existing report decision with one Requested execution row.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventReportDecisionExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_report_decisions_tenant_id_report_id_case_id",
                table: "event_report_decisions");

            migrationBuilder.AddColumn<Guid>(
                name: "current_decision_id",
                table: "event_report_cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "duplicate_group_id",
                table: "event_report_decisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE event_report_decisions AS decision
                SET duplicate_group_id = report.duplicate_group_id
                FROM event_reports AS report
                WHERE decision.tenant_id = report.tenant_id
                  AND decision.report_id = report.id
                  AND decision.decision_kind = 2
                  AND report.duplicate_group_id IS NOT NULL;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM event_report_decisions
                        WHERE decision_kind = 2
                          AND duplicate_group_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot persist duplicate decision identity: a historical duplicate decision has no duplicate group.'
                            USING HINT = 'Repair the owning report duplicate_group_id before applying this migration.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_report_decisions_duplicate_group_shape",
                table: "event_report_decisions",
                sql: "(decision_kind = 2 AND duplicate_group_id IS NOT NULL) OR (decision_kind <> 2 AND duplicate_group_id IS NULL)");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_event_moderation_records_tenant_id_id",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_event_report_decisions_tenant_report_case_id",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "report_id", "case_id", "id" });

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM event_moderation_records
                        WHERE source_report_id IS NOT NULL
                          AND source_report_decision_id IS NOT NULL
                        GROUP BY tenant_id, source_report_id, source_report_decision_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot enforce exact report-decision moderation receipt uniqueness: duplicate audit rows exist.'
                            USING HINT = 'Remediate duplicate tenant/report/decision moderation history explicitly before applying this migration; audit rows are not deleted automatically.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "ix_event_moderation_records_tenant_source_report_decision_exact",
                table: "event_moderation_records");

            migrationBuilder.CreateTable(
                name: "event_report_decision_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    enforcement_receipt_kind = table.Column<int>(type: "integer", nullable: false),
                    enforcement_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_lease_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_failure_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    enforcement_completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_report_decision_executions", x => x.id);
                    table.UniqueConstraint("ak_event_report_decision_executions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_report_decision_executions_failure_code_not_blank", "last_failure_code IS NULL OR length(btrim(last_failure_code)) > 0");
                    table.CheckConstraint("ck_event_report_decision_executions_lease_pair", "(processing_lease_token IS NULL) = (processing_lease_expires_at_utc IS NULL)");
                    table.CheckConstraint("ck_event_report_decision_executions_moderation_record_shape", "(enforcement_receipt_kind IN (2, 3) AND moderation_record_id IS NOT NULL AND moderation_record_id = enforcement_receipt_id) OR (enforcement_receipt_kind NOT IN (2, 3) AND moderation_record_id IS NULL)");
                    table.CheckConstraint("ck_event_report_decision_executions_receipt_id_shape", "(enforcement_receipt_kind IN (2, 3) AND enforcement_receipt_id IS NOT NULL) OR (enforcement_receipt_kind NOT IN (2, 3) AND enforcement_receipt_id IS NULL)");
                    table.CheckConstraint("ck_event_report_decision_executions_receipt_kind", "enforcement_receipt_kind BETWEEN 0 AND 5");
                    table.CheckConstraint("ck_event_report_decision_executions_state", "state BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_event_report_decision_executions_state_shape", "(state = 1 AND enforcement_receipt_kind = 0 AND enforcement_completed_at_utc IS NULL AND completed_at_utc IS NULL AND processing_lease_token IS NULL) OR (state = 2 AND enforcement_receipt_kind = 0 AND enforcement_completed_at_utc IS NULL AND completed_at_utc IS NULL AND processing_lease_token IS NOT NULL) OR (state = 3 AND enforcement_receipt_kind <> 0 AND enforcement_completed_at_utc IS NOT NULL AND completed_at_utc IS NULL) OR (state = 4 AND enforcement_receipt_kind <> 0 AND enforcement_completed_at_utc IS NOT NULL AND completed_at_utc IS NOT NULL AND processing_lease_token IS NULL)");
                    table.ForeignKey(
                        name: "fk_event_report_decision_executions_event_moderation_records_t",
                        columns: x => new { x.tenant_id, x.moderation_record_id },
                        principalTable: "event_moderation_records",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_decision_executions_event_report_decisions_ten",
                        columns: x => new { x.tenant_id, x.report_id, x.decision_id },
                        principalTable: "event_report_decisions",
                        principalColumns: new[] { "tenant_id", "report_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_decision_executions_event_reports_tenant_id_re",
                        columns: x => new { x.tenant_id, x.report_id },
                        principalTable: "event_reports",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_report_decision_executions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO event_report_decision_executions
                    (id, tenant_id, report_id, decision_id, state, enforcement_receipt_kind,
                     attempt_count, created_at, version)
                SELECT uuidv7(), tenant_id, report_id, id, 1, 0, 0, created_at, 1
                FROM event_report_decisions;
                """);

            migrationBuilder.Sql(
                """
                WITH latest_decisions AS (
                    SELECT DISTINCT ON (tenant_id, report_id, case_id)
                        tenant_id, report_id, case_id, id
                    FROM event_report_decisions
                    ORDER BY tenant_id, report_id, case_id, created_at DESC, id DESC
                )
                UPDATE event_report_cases AS report_case
                SET current_decision_id = latest.id
                FROM latest_decisions AS latest
                WHERE report_case.tenant_id = latest.tenant_id
                  AND report_case.report_id = latest.report_id
                  AND report_case.id = latest.case_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_event_moderation_records_tenant_source_report_decision_exact",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_id", "source_report_decision_id" },
                unique: true,
                filter: "source_report_id IS NOT NULL AND source_report_decision_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_event_moderation_records_exact_receipt_fk",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_id", "source_report_decision_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_report_decision_executions_runnable",
                table: "event_report_decision_executions",
                columns: new[] { "state", "processing_lease_expires_at_utc", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_event_report_decision_executions_tenant_report_decision",
                table: "event_report_decision_executions",
                columns: new[] { "tenant_id", "report_id", "decision_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_event_report_decision_executions_tenant_decision",
                table: "event_report_decision_executions",
                columns: new[] { "tenant_id", "decision_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_report_decision_executions_tenant_id_moderation_record_id",
                table: "event_report_decision_executions",
                columns: new[] { "tenant_id", "moderation_record_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_cases_current_decision",
                table: "event_report_cases",
                columns: new[] { "tenant_id", "report_id", "id", "current_decision_id" },
                filter: "current_decision_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_event_report_cases_event_report_decisions_tenant_id_report_",
                table: "event_report_cases",
                columns: new[] { "tenant_id", "report_id", "id", "current_decision_id" },
                principalTable: "event_report_decisions",
                principalColumns: new[] { "tenant_id", "report_id", "case_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                ALTER TABLE event_report_decision_executions
                ADD CONSTRAINT fk_event_report_decision_executions_exact_moderation_receipt
                FOREIGN KEY (tenant_id, report_id, decision_id, moderation_record_id)
                REFERENCES event_moderation_records
                    (tenant_id, source_report_id, source_report_decision_id, id)
                ON DELETE RESTRICT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE event_report_decision_executions
                DROP CONSTRAINT IF EXISTS fk_event_report_decision_executions_exact_moderation_receipt;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_event_report_cases_event_report_decisions_tenant_id_report_",
                table: "event_report_cases");

            migrationBuilder.DropTable(
                name: "event_report_decision_executions");

            migrationBuilder.DropIndex(
                name: "ix_event_report_cases_current_decision",
                table: "event_report_cases");

            migrationBuilder.DropIndex(
                name: "ux_event_moderation_records_exact_receipt_fk",
                table: "event_moderation_records");

            migrationBuilder.DropIndex(
                name: "ux_event_moderation_records_tenant_source_report_decision_exact",
                table: "event_moderation_records");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_event_report_decisions_tenant_report_case_id",
                table: "event_report_decisions");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_event_moderation_records_tenant_id_id",
                table: "event_moderation_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_report_decisions_duplicate_group_shape",
                table: "event_report_decisions");

            migrationBuilder.DropColumn(
                name: "duplicate_group_id",
                table: "event_report_decisions");

            migrationBuilder.DropColumn(
                name: "current_decision_id",
                table: "event_report_cases");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_decisions_tenant_id_report_id_case_id",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "report_id", "case_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_tenant_source_report_decision_exact",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_id", "source_report_decision_id" },
                filter: "source_report_id IS NOT NULL AND source_report_decision_id IS NOT NULL");
        }
    }
}
