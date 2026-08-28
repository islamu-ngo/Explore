using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddFairReturnOrchestration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provider_idempotency_key",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "refund_attempt_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "stable_operation_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "waitlist_payment_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fair_return_source_binding_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    replacement_payment_attempt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reserved_refund_attempt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    original_payment_allocation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stable_operation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    refund_intent_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_idempotency_key = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    replacement_payment_settled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waitlist_payment_intents", x => x.id);
                    table.UniqueConstraint("ak_waitlist_payment_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_waitlist_payment_intents_fair_return_source_bindings_tenant_id_fair_return_source_binding_id",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalSchema: "islamu_event",
                        principalTable: "fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waitlist_payment_intents_payment_attempts_tenant_id_replacement_payment_attempt_id",
                        columns: x => new { x.tenant_id, x.replacement_payment_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waitlist_payment_intents_refund_attempts_tenant_id_reserved_refund_attempt_id",
                        columns: x => new { x.tenant_id, x.reserved_refund_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "refund_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waitlist_payment_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fair_return_orchestration_effects",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    waitlist_payment_intent_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stable_operation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stable_cursor = table.Column<long>(type: "bigint", nullable: false),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    lease_expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    lease_owner = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    maximum_attempts = table.Column<int>(type: "int", nullable: false),
                    last_failure_code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fair_return_orchestration_effects", x => x.id);
                    table.UniqueConstraint("ak_fair_return_orchestration_effects_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_effect_attempts", "attempt_count >= 0 AND maximum_attempts BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_fair_return_effect_state", "(status_id = 1 AND lease_expires_at IS NULL AND lease_owner IS NULL AND completed_at IS NULL AND dead_lettered_at IS NULL) OR (status_id = 2 AND lease_expires_at IS NOT NULL AND lease_owner IS NOT NULL AND completed_at IS NULL AND dead_lettered_at IS NULL) OR (status_id = 3 AND lease_expires_at IS NULL AND lease_owner IS NULL AND completed_at IS NOT NULL AND dead_lettered_at IS NULL) OR (status_id = 4 AND lease_expires_at IS NULL AND lease_owner IS NULL AND completed_at IS NULL AND dead_lettered_at IS NOT NULL)");
                    table.CheckConstraint("ck_fair_return_effect_status", "status_id BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_fair_return_orchestration_effects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fair_return_orchestration_effects_waitlist_payment_intents_tenant_id_waitlist_payment_intent_id",
                        columns: x => new { x.tenant_id, x.waitlist_payment_intent_id },
                        principalSchema: "islamu_event",
                        principalTable: "waitlist_payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_refund_intents_tenant_id_refund_attempt_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                columns: new[] { "tenant_id", "refund_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_refund_intents_tenant_id_stable_operation_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                columns: new[] { "tenant_id", "stable_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_orchestration_effects_stable_cursor_id",
                schema: "islamu_event",
                table: "fair_return_orchestration_effects",
                columns: new[] { "stable_cursor", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_orchestration_effects_status_id_next_attempt_at_created_at_id",
                schema: "islamu_event",
                table: "fair_return_orchestration_effects",
                columns: new[] { "status_id", "next_attempt_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_orchestration_effects_tenant_id",
                schema: "islamu_event",
                table: "fair_return_orchestration_effects",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_orchestration_effects_tenant_id_stable_operation_id",
                schema: "islamu_event",
                table: "fair_return_orchestration_effects",
                columns: new[] { "tenant_id", "stable_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fair_return_orchestration_effects_tenant_id_waitlist_payment_intent_id",
                schema: "islamu_event",
                table: "fair_return_orchestration_effects",
                columns: new[] { "tenant_id", "waitlist_payment_intent_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_payment_intents_tenant_id",
                schema: "islamu_event",
                table: "waitlist_payment_intents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_payment_intents_tenant_id_fair_return_source_binding_id",
                schema: "islamu_event",
                table: "waitlist_payment_intents",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_payment_intents_tenant_id_replacement_payment_attempt_id",
                schema: "islamu_event",
                table: "waitlist_payment_intents",
                columns: new[] { "tenant_id", "replacement_payment_attempt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_payment_intents_tenant_id_reserved_refund_attempt_id",
                schema: "islamu_event",
                table: "waitlist_payment_intents",
                columns: new[] { "tenant_id", "reserved_refund_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_payment_intents_tenant_id_stable_operation_id",
                schema: "islamu_event",
                table: "waitlist_payment_intents",
                columns: new[] { "tenant_id", "stable_operation_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_waitlist_refund_intents_refund_attempts_tenant_id_refund_attempt_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents",
                columns: new[] { "tenant_id", "refund_attempt_id" },
                principalSchema: "islamu_event",
                principalTable: "refund_attempts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_waitlist_refund_intents_refund_attempts_tenant_id_refund_attempt_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents");

            migrationBuilder.DropTable(
                name: "fair_return_orchestration_effects",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "waitlist_payment_intents",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_waitlist_refund_intents_tenant_id_refund_attempt_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents");

            migrationBuilder.DropIndex(
                name: "ix_waitlist_refund_intents_tenant_id_stable_operation_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents");

            migrationBuilder.DropColumn(
                name: "provider_idempotency_key",
                schema: "islamu_event",
                table: "waitlist_refund_intents");

            migrationBuilder.DropColumn(
                name: "refund_attempt_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents");

            migrationBuilder.DropColumn(
                name: "stable_operation_id",
                schema: "islamu_event",
                table: "waitlist_refund_intents");
        }
    }
}
