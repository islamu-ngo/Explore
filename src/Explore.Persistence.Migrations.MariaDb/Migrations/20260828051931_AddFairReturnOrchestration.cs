using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddFairReturnOrchestration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provider_idempotency_key",
                table: "ie_waitlist_refund_intents",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "refund_attempt_id",
                table: "ie_waitlist_refund_intents",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "stable_operation_id",
                table: "ie_waitlist_refund_intents",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "ie_waitlist_payment_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    fair_return_source_binding_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    replacement_payment_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    reserved_refund_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    original_payment_allocation_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    stable_operation_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    refund_intent_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    provider_idempotency_key = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    replacement_payment_settled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_waitlist_payment_intents", x => x.id);
                    table.UniqueConstraint("ak_waitlist_payment_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_waitlist_payment_intents_ie_fair_return_source_bi_E18E7DCF",
                        columns: x => new { x.tenant_id, x.fair_return_source_binding_id },
                        principalTable: "ie_fair_return_source_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_waitlist_payment_intents_ie_payment_attempts_tena_B480DAE5",
                        columns: x => new { x.tenant_id, x.replacement_payment_attempt_id },
                        principalTable: "ie_payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_waitlist_payment_intents_ie_refund_attempts_tenan_B15B4FAF",
                        columns: x => new { x.tenant_id, x.reserved_refund_attempt_id },
                        principalTable: "ie_refund_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_waitlist_payment_intents_ie_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_fair_return_orchestration_effects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    waitlist_payment_intent_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    stable_operation_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    stable_cursor = table.Column<long>(type: "bigint", nullable: false),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    lease_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    lease_owner = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    maximum_attempts = table.Column<int>(type: "int", nullable: false),
                    last_failure_code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_fair_return_orchestration_effects", x => x.id);
                    table.UniqueConstraint("ak_fair_return_orchestration_effects_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_fair_return_effect_attempts", "attempt_count >= 0 AND maximum_attempts BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_fair_return_effect_state", "(status_id = 1 AND lease_expires_at IS NULL AND lease_owner IS NULL AND completed_at IS NULL AND dead_lettered_at IS NULL) OR (status_id = 2 AND lease_expires_at IS NOT NULL AND lease_owner IS NOT NULL AND completed_at IS NULL AND dead_lettered_at IS NULL) OR (status_id = 3 AND lease_expires_at IS NULL AND lease_owner IS NULL AND completed_at IS NOT NULL AND dead_lettered_at IS NULL) OR (status_id = 4 AND lease_expires_at IS NULL AND lease_owner IS NULL AND completed_at IS NULL AND dead_lettered_at IS NOT NULL)");
                    table.CheckConstraint("ck_fair_return_effect_status", "status_id BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_ie_fair_return_orchestration_effects_ie_waitlist_pay_A4B1FB1F",
                        columns: x => new { x.tenant_id, x.waitlist_payment_intent_id },
                        principalTable: "ie_waitlist_payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_fair_return_orchestration_effects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_waitlist_refund_intents_tenant_id_refund_attempt_id",
                table: "ie_waitlist_refund_intents",
                columns: new[] { "tenant_id", "refund_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_waitlist_refund_intents_tenant_id_stable_operation_id",
                table: "ie_waitlist_refund_intents",
                columns: new[] { "tenant_id", "stable_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_fair_return_orchestration_effects_stable_cursor_id",
                table: "ie_fair_return_orchestration_effects",
                columns: new[] { "stable_cursor", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_orchestration_effects_status_id_next__04995318",
                table: "ie_fair_return_orchestration_effects",
                columns: new[] { "status_id", "next_attempt_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_fair_return_orchestration_effects_tenant_id",
                table: "ie_fair_return_orchestration_effects",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_orchestration_effects_tenant_id_stabl_055F60DB",
                table: "ie_fair_return_orchestration_effects",
                columns: new[] { "tenant_id", "stable_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_fair_return_orchestration_effects_tenant_id_waitl_53044224",
                table: "ie_fair_return_orchestration_effects",
                columns: new[] { "tenant_id", "waitlist_payment_intent_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_waitlist_payment_intents_tenant_id",
                table: "ie_waitlist_payment_intents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_waitlist_payment_intents_tenant_id_fair_return_so_4B586086",
                table: "ie_waitlist_payment_intents",
                columns: new[] { "tenant_id", "fair_return_source_binding_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_waitlist_payment_intents_tenant_id_replacement_pa_565D4450",
                table: "ie_waitlist_payment_intents",
                columns: new[] { "tenant_id", "replacement_payment_attempt_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_waitlist_payment_intents_tenant_id_reserved_refun_C178BE3B",
                table: "ie_waitlist_payment_intents",
                columns: new[] { "tenant_id", "reserved_refund_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_waitlist_payment_intents_tenant_id_stable_operation_id",
                table: "ie_waitlist_payment_intents",
                columns: new[] { "tenant_id", "stable_operation_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_waitlist_refund_intents_ie_refund_attempts_tenant_89391695",
                table: "ie_waitlist_refund_intents",
                columns: new[] { "tenant_id", "refund_attempt_id" },
                principalTable: "ie_refund_attempts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ie_waitlist_refund_intents_ie_refund_attempts_tenant_89391695",
                table: "ie_waitlist_refund_intents");

            migrationBuilder.DropTable(
                name: "ie_fair_return_orchestration_effects");

            migrationBuilder.DropTable(
                name: "ie_waitlist_payment_intents");

            migrationBuilder.DropIndex(
                name: "ix_ie_waitlist_refund_intents_tenant_id_refund_attempt_id",
                table: "ie_waitlist_refund_intents");

            migrationBuilder.DropIndex(
                name: "ix_ie_waitlist_refund_intents_tenant_id_stable_operation_id",
                table: "ie_waitlist_refund_intents");

            migrationBuilder.DropColumn(
                name: "provider_idempotency_key",
                table: "ie_waitlist_refund_intents");

            migrationBuilder.DropColumn(
                name: "refund_attempt_id",
                table: "ie_waitlist_refund_intents");

            migrationBuilder.DropColumn(
                name: "stable_operation_id",
                table: "ie_waitlist_refund_intents");
        }
    }
}
