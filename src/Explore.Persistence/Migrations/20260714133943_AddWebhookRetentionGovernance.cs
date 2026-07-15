using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookRetentionGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "attempt_retention_until_utc",
                table: "webhook_delivery_plan_snapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '30 days'");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dead_letter_evidence_retention_until_utc",
                table: "webhook_delivery_plan_snapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '90 days'");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "operational_log_retention_until_utc",
                table: "webhook_delivery_plan_snapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '30 days'");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "publication_retention_until_utc",
                table: "webhook_delivery_plan_snapshots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '90 days'");

            migrationBuilder.AddColumn<string>(
                name: "retention_policy_version",
                table: "webhook_audit_events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "legacy-retention-v1");

            migrationBuilder.AddColumn<DateTime>(
                name: "retention_until",
                table: "webhook_audit_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '365 days'");

            migrationBuilder.AddColumn<DateTime>(
                name: "dead_letter_evidence_retention_until",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '90 days'");

            migrationBuilder.AddColumn<DateTime>(
                name: "operational_log_retention_until",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '30 days'");

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_attempt_retention_until",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '30 days'");

            migrationBuilder.AddColumn<DateTime>(
                name: "replay_window_until",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp() + INTERVAL '14 days'");

            migrationBuilder.AddColumn<string>(
                name: "retention_policy_version",
                table: "incoming_webhook_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "legacy-retention-v1");

            migrationBuilder.CreateTable(
                name: "webhook_retention_subject_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_retention_subject_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_retention_holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_kind_id = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    placed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_retention_holds", x => x.id);
                    table.UniqueConstraint("ak_webhook_retention_holds_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_webhook_retention_holds_expiry", "expires_at IS NULL OR expires_at > placed_at");
                    table.CheckConstraint("ck_webhook_retention_holds_release", "released_at IS NULL OR released_at >= placed_at");
                    table.ForeignKey(
                        name: "fk_webhook_retention_holds_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_retention_holds_webhook_retention_subject_kinds_sub",
                        column: x => x.subject_kind_id,
                        principalTable: "webhook_retention_subject_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_attempt_retention",
                table: "webhook_delivery_plan_snapshots",
                columns: new[] { "tenant_id", "attempt_retention_until_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_dead_letter_retention",
                table: "webhook_delivery_plan_snapshots",
                columns: new[] { "tenant_id", "dead_letter_evidence_retention_until_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_publication_retention",
                table: "webhook_delivery_plan_snapshots",
                columns: new[] { "tenant_id", "publication_retention_until_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_audit_events_tenant_retention",
                table: "webhook_audit_events",
                columns: new[] { "tenant_id", "retention_until" });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_attempt_retention",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "processing_attempt_retention_until" });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_dead_letter_retention",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "dead_letter_evidence_retention_until" });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_payload_retention",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "payload_retention_until", "replay_window_until" },
                filter: "payload_bytes IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_retention_holds_subject_kind_id",
                table: "webhook_retention_holds",
                column: "subject_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_retention_holds_tenant_subject_active",
                table: "webhook_retention_holds",
                columns: new[] { "tenant_id", "subject_kind_id", "subject_id", "released_at", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_retention_subject_kinds_master_code",
                table: "webhook_retention_subject_kinds",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_retention_holds");

            migrationBuilder.DropTable(
                name: "webhook_retention_subject_kinds");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_attempt_retention",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_dead_letter_retention",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_publication_retention",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_webhook_audit_events_tenant_retention",
                table: "webhook_audit_events");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_attempt_retention",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_dead_letter_retention",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_payload_retention",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "attempt_retention_until_utc",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropColumn(
                name: "dead_letter_evidence_retention_until_utc",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropColumn(
                name: "operational_log_retention_until_utc",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropColumn(
                name: "publication_retention_until_utc",
                table: "webhook_delivery_plan_snapshots");

            migrationBuilder.DropColumn(
                name: "retention_policy_version",
                table: "webhook_audit_events");

            migrationBuilder.DropColumn(
                name: "retention_until",
                table: "webhook_audit_events");

            migrationBuilder.DropColumn(
                name: "dead_letter_evidence_retention_until",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "operational_log_retention_until",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "processing_attempt_retention_until",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "replay_window_until",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "retention_policy_version",
                table: "incoming_webhook_messages");
        }
    }
}
