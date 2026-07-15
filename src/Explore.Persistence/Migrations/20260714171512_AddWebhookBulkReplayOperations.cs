using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookBulkReplayOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_bulk_replay_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_bulk_replay_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_bulk_replay_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_key = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(71)", maxLength: 71, nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    to_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    webhook_consumer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    webhook_endpoint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    requested_max_items = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cancellation_reason_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    estimated_eligible_count = table.Column<int>(type: "integer", nullable: false),
                    estimated_selected_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_held_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_payload_unavailable_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_endpoint_unavailable_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_ineligible_local_state_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_provider_conflict_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_provider_unknown_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_provider_manual_reconciliation_count = table.Column<int>(type: "integer", nullable: false),
                    excluded_provider_ineligible_count = table.Column<int>(type: "integer", nullable: false),
                    scheduled_count = table.Column<int>(type: "integer", nullable: false),
                    failure_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concurrency_version = table.Column<long>(type: "bigint", nullable: false),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_bulk_replay_operations", x => x.id);
                    table.UniqueConstraint("ak_webhook_bulk_replay_operations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_webhook_bulk_replay_operations_concurrency_version", "concurrency_version > 0");
                    table.CheckConstraint("ck_webhook_bulk_replay_operations_filter_window", "to_utc > from_utc");
                    table.CheckConstraint("ck_webhook_bulk_replay_operations_lifecycle", "(status_id = 1 AND started_at IS NULL AND completed_at IS NULL AND cancelled_at IS NULL AND failed_at IS NULL AND failure_code IS NULL) OR (status_id = 2 AND started_at IS NOT NULL AND completed_at IS NULL AND cancelled_at IS NULL AND failed_at IS NULL AND failure_code IS NULL) OR (status_id = 3 AND started_at IS NOT NULL AND completed_at IS NOT NULL AND cancelled_at IS NULL AND failed_at IS NULL AND failure_code IS NULL) OR (status_id = 4 AND started_at IS NULL AND completed_at IS NULL AND cancelled_at IS NOT NULL AND failed_at IS NULL AND failure_code IS NULL) OR (status_id = 5 AND started_at IS NOT NULL AND completed_at IS NULL AND cancelled_at IS NULL AND failed_at IS NOT NULL AND failure_code IS NOT NULL)");
                    table.CheckConstraint("ck_webhook_bulk_replay_operations_nonnegative_counts", "estimated_eligible_count >= 0 AND estimated_selected_count >= 0 AND excluded_held_count >= 0 AND excluded_payload_unavailable_count >= 0 AND excluded_endpoint_unavailable_count >= 0 AND excluded_ineligible_local_state_count >= 0 AND excluded_provider_conflict_count >= 0 AND excluded_provider_unknown_count >= 0 AND excluded_provider_manual_reconciliation_count >= 0 AND excluded_provider_ineligible_count >= 0 AND scheduled_count >= 0");
                    table.CheckConstraint("ck_webhook_bulk_replay_operations_request_hash", "request_hash ~ '^sha256:[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_webhook_bulk_replay_operations_requested_max", "requested_max_items BETWEEN 1 AND 1000");
                    table.CheckConstraint("ck_webhook_bulk_replay_operations_selected_bounds", "estimated_selected_count <= requested_max_items AND scheduled_count <= requested_max_items");
                    table.ForeignKey(
                        name: "fk_webhook_bulk_replay_operations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_bulk_replay_operations_webhook_bulk_replay_statuses",
                        column: x => x.status_id,
                        principalTable: "webhook_bulk_replay_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_bulk_replay_operations_webhook_consumers_tenant_id_",
                        columns: x => new { x.tenant_id, x.webhook_consumer_id },
                        principalTable: "webhook_consumers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_bulk_replay_operations_webhook_endpoints_tenant_id_",
                        columns: x => new { x.tenant_id, x.webhook_endpoint_id },
                        principalTable: "webhook_endpoints",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_status_queue",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "status_id", "queued_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_id_webhook_consumer_id",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "webhook_consumer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_id_webhook_endpoint_id",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "webhook_endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_status_queue",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "status_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_bulk_replay_operations_tenant_window",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "from_utc", "to_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_bulk_replay_operations_tenant_operation_key",
                table: "webhook_bulk_replay_operations",
                columns: new[] { "tenant_id", "operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_bulk_replay_statuses_master_code",
                table: "webhook_bulk_replay_statuses",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_bulk_replay_operations");

            migrationBuilder.DropTable(
                name: "webhook_bulk_replay_statuses");
        }
    }
}
