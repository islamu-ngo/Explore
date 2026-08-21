using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentWebhookReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_provider_idempotency",
                schema: "islamu_event",
                table: "incoming_webhook_messages");

            migrationBuilder.CreateTable(
                name: "payment_reconciliation_effects",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_incoming_webhook_message_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    checkout_dispatch_effect_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    checkout_dispatch_unknown_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    checkout_dispatch_processing_fence = table.Column<long>(type: "bigint", nullable: true),
                    checkout_dispatch_attempt_count = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    processing_lease_owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processing_lease_expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    next_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    parked_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    unknown_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_failure_code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_reconciliation_effects", x => x.id);
                    table.UniqueConstraint("ak_payment_reconciliation_effects_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_payment_reconciliation_effects_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_payment_reconciliation_effects_dispatch_unknown_epoch", "(checkout_dispatch_effect_id IS NULL AND checkout_dispatch_unknown_at IS NULL AND checkout_dispatch_processing_fence IS NULL AND checkout_dispatch_attempt_count IS NULL) OR (checkout_dispatch_effect_id IS NOT NULL AND checkout_dispatch_unknown_at IS NOT NULL AND checkout_dispatch_processing_fence >= 0 AND checkout_dispatch_attempt_count >= 0)");
                    table.CheckConstraint("ck_payment_reconciliation_effects_processing_fence", "processing_fence >= 0");
                    table.ForeignKey(
                        name: "fk_payment_reconciliation_effects_checkout_dispatch_effects_tenant_id_checkout_dispatch_effect_id",
                        columns: x => new { x.tenant_id, x.checkout_dispatch_effect_id },
                        principalSchema: "islamu_event",
                        principalTable: "checkout_dispatch_effects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_reconciliation_effects_incoming_webhook_messages_tenant_id_source_incoming_webhook_message_id",
                        columns: x => new { x.tenant_id, x.source_incoming_webhook_message_id },
                        principalSchema: "islamu_event",
                        principalTable: "incoming_webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_reconciliation_effects_payment_attempts_tenant_id_payment_attempt_id",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_reconciliation_effects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_succeeded_observations",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_incoming_webhook_message_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    provider_checkout_session_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    provider_payment_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    provider_request_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    observed_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_succeeded_observations", x => x.id);
                    table.UniqueConstraint("ak_payment_succeeded_observations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_payment_succeeded_observations_incoming_webhook_messages_tenant_id_source_incoming_webhook_message_id",
                        columns: x => new { x.tenant_id, x.source_incoming_webhook_message_id },
                        principalSchema: "islamu_event",
                        principalTable: "incoming_webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_succeeded_observations_payment_attempts_tenant_id_payment_attempt_id",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_succeeded_observations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_provider_idempotency",
                schema: "islamu_event",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "provider", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_status_next_attempt_at_created_at",
                schema: "islamu_event",
                table: "payment_reconciliation_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_tenant_id_checkout_dispatch_effect_id",
                schema: "islamu_event",
                table: "payment_reconciliation_effects",
                columns: new[] { "tenant_id", "checkout_dispatch_effect_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_tenant_id_payment_attempt_id",
                schema: "islamu_event",
                table: "payment_reconciliation_effects",
                columns: new[] { "tenant_id", "payment_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_tenant_id_source_incoming_webhook_message_id",
                schema: "islamu_event",
                table: "payment_reconciliation_effects",
                columns: new[] { "tenant_id", "source_incoming_webhook_message_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_succeeded_observations_tenant_id_payment_attempt_id",
                schema: "islamu_event",
                table: "payment_succeeded_observations",
                columns: new[] { "tenant_id", "payment_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_succeeded_observations_tenant_id_source_incoming_webhook_message_id",
                schema: "islamu_event",
                table: "payment_succeeded_observations",
                columns: new[] { "tenant_id", "source_incoming_webhook_message_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_reconciliation_effects",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "payment_succeeded_observations",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_provider_idempotency",
                schema: "islamu_event",
                table: "incoming_webhook_messages");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_provider_idempotency",
                schema: "islamu_event",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "provider", "idempotency_key" },
                filter: "idempotency_key IS NOT NULL");
        }
    }
}
