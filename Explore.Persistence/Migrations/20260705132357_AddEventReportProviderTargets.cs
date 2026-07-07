using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventReportProviderTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_report_signals_tenant_event_provider_created",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ix_event_report_signals_tenant_report_provider_created",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ux_event_report_signals_tenant_provider_correlation",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ux_event_report_signals_tenant_provider_external_signal",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ix_event_report_external_links_tenant_provider_state_created",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_external_links_tenant_provider_case",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_external_links_tenant_provider_correlation",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_external_links_tenant_provider_signal",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_decisions_tenant_source_external",
                table: "event_report_decisions");

            migrationBuilder.AddColumn<string>(
                name: "provider_target_id",
                table: "event_report_signals",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "instance");

            migrationBuilder.AddColumn<int>(
                name: "provider_target_scope",
                table: "event_report_signals",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "provider_target_id",
                table: "event_report_external_links",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "instance");

            migrationBuilder.AddColumn<int>(
                name: "provider_target_scope",
                table: "event_report_external_links",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "provider_target_id",
                table: "event_report_decisions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "local");

            migrationBuilder.AddColumn<int>(
                name: "provider_target_scope",
                table: "event_report_decisions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_event_report_signals_tenant_event_provider_target_created",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "event_id", "provider", "provider_target_scope", "provider_target_id", "created_at" },
                descending: new[] { false, false, false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_event_report_signals_tenant_report_provider_target_created",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "report_id", "provider", "provider_target_scope", "provider_target_id", "created_at" },
                descending: new[] { false, false, false, false, false, true },
                filter: "report_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_event_report_signals_tenant_provider_target_correlation",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "provider", "provider_target_scope", "provider_target_id", "correlation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_event_report_signals_tenant_provider_target_external_signal",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "provider", "provider_target_scope", "provider_target_id", "external_signal_id" },
                unique: true,
                filter: "external_signal_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_report_signals_provider_target_id_not_blank",
                table: "event_report_signals",
                sql: "length(btrim(provider_target_id)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_report_signals_provider_target_scope",
                table: "event_report_signals",
                sql: "provider_target_scope BETWEEN 1 AND 3");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_external_links_tenant_provider_target_state_created",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "provider_target_scope", "provider_target_id", "sync_state", "created_at" },
                descending: new[] { false, false, false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_event_report_external_links_tenant_provider_target_case",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "provider_target_scope", "provider_target_id", "provider_case_id" },
                unique: true,
                filter: "provider_case_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_event_report_external_links_tenant_provider_target_correlation",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "provider_target_scope", "provider_target_id", "correlation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_event_report_external_links_tenant_provider_target_signal",
                table: "event_report_external_links",
                columns: new[] { "tenant_id", "provider", "provider_target_scope", "provider_target_id", "provider_signal_id" },
                unique: true,
                filter: "provider_signal_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_report_external_links_provider_target_id_not_blank",
                table: "event_report_external_links",
                sql: "length(btrim(provider_target_id)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_report_external_links_provider_target_scope",
                table: "event_report_external_links",
                sql: "provider_target_scope BETWEEN 1 AND 3");

            migrationBuilder.CreateIndex(
                name: "ux_event_report_decisions_tenant_source_target_external",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "decision_source", "provider_target_scope", "provider_target_id", "external_decision_id" },
                unique: true,
                filter: "external_decision_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_report_decisions_provider_target_id_not_blank",
                table: "event_report_decisions",
                sql: "length(btrim(provider_target_id)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_report_decisions_provider_target_scope",
                table: "event_report_decisions",
                sql: "provider_target_scope BETWEEN 1 AND 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_report_signals_tenant_event_provider_target_created",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ix_event_report_signals_tenant_report_provider_target_created",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ux_event_report_signals_tenant_provider_target_correlation",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ux_event_report_signals_tenant_provider_target_external_signal",
                table: "event_report_signals");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_report_signals_provider_target_id_not_blank",
                table: "event_report_signals");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_report_signals_provider_target_scope",
                table: "event_report_signals");

            migrationBuilder.DropIndex(
                name: "ix_event_report_external_links_tenant_provider_target_state_created",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_external_links_tenant_provider_target_case",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_external_links_tenant_provider_target_correlation",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_external_links_tenant_provider_target_signal",
                table: "event_report_external_links");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_report_external_links_provider_target_id_not_blank",
                table: "event_report_external_links");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_report_external_links_provider_target_scope",
                table: "event_report_external_links");

            migrationBuilder.DropIndex(
                name: "ux_event_report_decisions_tenant_source_target_external",
                table: "event_report_decisions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_report_decisions_provider_target_id_not_blank",
                table: "event_report_decisions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_report_decisions_provider_target_scope",
                table: "event_report_decisions");

            migrationBuilder.DropColumn(
                name: "provider_target_id",
                table: "event_report_signals");

            migrationBuilder.DropColumn(
                name: "provider_target_scope",
                table: "event_report_signals");

            migrationBuilder.DropColumn(
                name: "provider_target_id",
                table: "event_report_external_links");

            migrationBuilder.DropColumn(
                name: "provider_target_scope",
                table: "event_report_external_links");

            migrationBuilder.DropColumn(
                name: "provider_target_id",
                table: "event_report_decisions");

            migrationBuilder.DropColumn(
                name: "provider_target_scope",
                table: "event_report_decisions");

            migrationBuilder.CreateIndex(
                name: "ix_event_report_signals_tenant_event_provider_created",
                table: "event_report_signals",
                columns: new[] { "tenant_id", "event_id", "provider", "created_at" },
                descending: new[] { false, false, false, true });

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
                name: "ux_event_report_decisions_tenant_source_external",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "decision_source", "external_decision_id" },
                unique: true,
                filter: "external_decision_id IS NOT NULL");
        }
    }
}
