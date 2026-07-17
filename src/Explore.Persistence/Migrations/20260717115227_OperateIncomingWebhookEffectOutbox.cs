using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OperateIncomingWebhookEffectOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_effect_outbox_worker_poll",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "incoming_webhook_effect_outbox",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "incoming_webhook_effect_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dead_lettered_at",
                table: "incoming_webhook_effect_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "failure_category",
                table: "incoming_webhook_effect_outbox",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "incoming_webhook_effect_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "processing_fence",
                table: "incoming_webhook_effect_outbox",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "processing_generation",
                table: "incoming_webhook_effect_outbox",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_lease_expires_at",
                table: "incoming_webhook_effect_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_lease_owner",
                table: "incoming_webhook_effect_outbox",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "processing_lease_token",
                table: "incoming_webhook_effect_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_started_at",
                table: "incoming_webhook_effect_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "safe_detail",
                table: "incoming_webhook_effect_outbox",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_effect_outbox_worker_poll",
                table: "incoming_webhook_effect_outbox",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_attempt_count",
                table: "incoming_webhook_effect_outbox",
                sql: "attempt_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_failure_category",
                table: "incoming_webhook_effect_outbox",
                sql: "failure_category IS NULL OR failure_category ~ '^[a-z0-9_]+$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_processing_fence",
                table: "incoming_webhook_effect_outbox",
                sql: "processing_fence >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_processing_generation",
                table: "incoming_webhook_effect_outbox",
                sql: "processing_generation >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_effect_outbox_worker_poll",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_attempt_count",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_failure_category",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_processing_fence",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_effect_outbox_processing_generation",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "dead_lettered_at",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "failure_category",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "processing_fence",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "processing_generation",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "processing_lease_expires_at",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "processing_lease_owner",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "processing_lease_token",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "processing_started_at",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.DropColumn(
                name: "safe_detail",
                table: "incoming_webhook_effect_outbox");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_effect_outbox_worker_poll",
                table: "incoming_webhook_effect_outbox",
                columns: new[] { "status", "created_at" });
        }
    }
}
