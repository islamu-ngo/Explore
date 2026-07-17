using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationFanoutAudienceExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notification_fanout_runs_worker_poll",
                table: "notification_fanout_runs");

            migrationBuilder.DropIndex(
                name: "ux_notification_fanout_runs_source",
                table: "notification_fanout_runs");

            migrationBuilder.AddColumn<DateTime>(
                name: "cursor_first_eligible_registration_created_at",
                table: "notification_fanout_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cursor_user_id",
                table: "notification_fanout_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fanout_occurrence_id",
                table: "notification_fanout_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "heartbeat_at",
                table: "notification_fanout_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "processing_fence",
                table: "notification_fanout_runs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "processing_generation",
                table: "notification_fanout_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_lease_expires_at",
                table: "notification_fanout_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_lease_owner",
                table: "notification_fanout_runs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "processing_lease_token",
                table: "notification_fanout_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "coverage_established_at",
                table: "event_registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE event_registrations
                SET coverage_established_at = created_at
                WHERE coverage_established_at IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "coverage_established_at",
                table: "event_registrations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_runs_worker_poll",
                table: "notification_fanout_runs",
                columns: new[] { "status", "processing_lease_expires_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_fanout_runs_occurrence",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "fanout_occurrence_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_fanout_runs_source",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "fanout_kind", "notification_entity_type_id", "entity_id", "source_actor_id" },
                unique: true,
                filter: "fanout_occurrence_id IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_fanout_runs_cursor_pair",
                table: "notification_fanout_runs",
                sql: "(cursor_first_eligible_registration_created_at IS NULL) = (cursor_user_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_fanout_runs_generation_nonnegative",
                table: "notification_fanout_runs",
                sql: "processing_generation >= 0 AND processing_fence >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notification_fanout_runs_occurrence_lease",
                table: "notification_fanout_runs",
                sql: "fanout_occurrence_id IS NULL OR (status = 'processing' AND processing_lease_owner IS NOT NULL AND btrim(processing_lease_owner) <> '' AND processing_lease_token IS NOT NULL AND processing_lease_expires_at IS NOT NULL) OR (status <> 'processing' AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_fanout_runs_occurrence_tenant",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "fanout_occurrence_id" },
                principalTable: "notification_fanout_occurrences",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fanout_runs_occurrence_tenant",
                table: "notification_fanout_runs");

            migrationBuilder.DropIndex(
                name: "ix_notification_fanout_runs_worker_poll",
                table: "notification_fanout_runs");

            migrationBuilder.DropIndex(
                name: "ux_notification_fanout_runs_occurrence",
                table: "notification_fanout_runs");

            migrationBuilder.DropIndex(
                name: "ux_notification_fanout_runs_source",
                table: "notification_fanout_runs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_fanout_runs_cursor_pair",
                table: "notification_fanout_runs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_fanout_runs_generation_nonnegative",
                table: "notification_fanout_runs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_notification_fanout_runs_occurrence_lease",
                table: "notification_fanout_runs");

            migrationBuilder.Sql("""
                DELETE FROM notification_fanout_runs
                WHERE fanout_occurrence_id IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "cursor_first_eligible_registration_created_at",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "cursor_user_id",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "fanout_occurrence_id",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "heartbeat_at",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "processing_fence",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "processing_generation",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "processing_lease_expires_at",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "processing_lease_owner",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "processing_lease_token",
                table: "notification_fanout_runs");

            migrationBuilder.DropColumn(
                name: "coverage_established_at",
                table: "event_registrations");

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_runs_worker_poll",
                table: "notification_fanout_runs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_fanout_runs_source",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "fanout_kind", "notification_entity_type_id", "entity_id", "source_actor_id" },
                unique: true);
        }
    }
}
