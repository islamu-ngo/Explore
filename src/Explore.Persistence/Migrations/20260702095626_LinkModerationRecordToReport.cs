// ABOUTME: EF Core migration linking moderation enforcement records back to report decisions.
// ABOUTME: Adds nullable tenant-safe report and decision references without changing existing records.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkModerationRecordToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_report_decision_id",
                table: "event_moderation_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_report_id",
                table: "event_moderation_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_event_report_decisions_tenant_id_report_id_id",
                table: "event_report_decisions",
                columns: new[] { "tenant_id", "report_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_tenant_source_report",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_id" },
                filter: "source_report_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_tenant_source_report_decision",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_decision_id" },
                filter: "source_report_decision_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_moderation_records_tenant_source_report_decision_exact",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_id", "source_report_decision_id" },
                filter: "source_report_id IS NOT NULL AND source_report_decision_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_moderation_records_source_decision_requires_report",
                table: "event_moderation_records",
                sql: "source_report_decision_id IS NULL OR source_report_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_event_moderation_records_event_report_decisions_tenant_id_s",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_id", "source_report_decision_id" },
                principalTable: "event_report_decisions",
                principalColumns: new[] { "tenant_id", "report_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_moderation_records_event_reports_tenant_id_source_rep",
                table: "event_moderation_records",
                columns: new[] { "tenant_id", "source_report_id" },
                principalTable: "event_reports",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_moderation_records_event_report_decisions_tenant_id_s",
                table: "event_moderation_records");

            migrationBuilder.DropForeignKey(
                name: "fk_event_moderation_records_event_reports_tenant_id_source_rep",
                table: "event_moderation_records");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_event_report_decisions_tenant_id_report_id_id",
                table: "event_report_decisions");

            migrationBuilder.DropIndex(
                name: "ix_event_moderation_records_tenant_source_report",
                table: "event_moderation_records");

            migrationBuilder.DropIndex(
                name: "ix_event_moderation_records_tenant_source_report_decision",
                table: "event_moderation_records");

            migrationBuilder.DropIndex(
                name: "ix_event_moderation_records_tenant_source_report_decision_exact",
                table: "event_moderation_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_moderation_records_source_decision_requires_report",
                table: "event_moderation_records");

            migrationBuilder.DropColumn(
                name: "source_report_decision_id",
                table: "event_moderation_records");

            migrationBuilder.DropColumn(
                name: "source_report_id",
                table: "event_moderation_records");
        }
    }
}
