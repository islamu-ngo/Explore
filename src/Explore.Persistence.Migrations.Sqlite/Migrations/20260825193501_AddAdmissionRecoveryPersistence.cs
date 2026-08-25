using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionRecoveryPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_admission_recovery_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recovery_request_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    purpose = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    capability_version = table.Column<int>(type: "INTEGER", nullable: false),
                    lookup_key_version = table.Column<int>(type: "INTEGER", nullable: false),
                    lookup_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 44, nullable: false),
                    locator_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 44, nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    rotated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    active_uniqueness_slot = table.Column<int>(type: "INTEGER", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_recovery_capabilities", x => x.id);
                    table.UniqueConstraint("ak_admission_recovery_capabilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_recovery_capabilities_lifecycle", "(consumed_at IS NULL OR rotated_at IS NULL) AND ((consumed_at IS NULL AND rotated_at IS NULL AND active_uniqueness_slot = 0) OR ((consumed_at IS NOT NULL OR rotated_at IS NOT NULL) AND active_uniqueness_slot = capability_version))");
                    table.CheckConstraint("ck_admission_recovery_capabilities_versions", "capability_version > 0 AND lookup_key_version > 0");
                    table.ForeignKey(
                        name: "fk_ie_admission_recovery_capabilities_admission_tickets_tenant_id_admission_ticket_id",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_recovery_capabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_admission_recovery_delivery_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recovery_request_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    purpose = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    capability_version = table.Column<int>(type: "INTEGER", nullable: false),
                    protected_material = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    protection_version = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    routed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    handoff_completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    handoff_receipt_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_recovery_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_recovery_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_recovery_delivery_intents_handoff", "(handoff_completed_at IS NULL AND handoff_receipt_id IS NULL) OR (handoff_completed_at IS NOT NULL AND handoff_receipt_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_recovery_delivery_intents_versions", "capability_version > 0 AND protection_version > 0");
                    table.ForeignKey(
                        name: "fk_ie_admission_recovery_delivery_intents_admission_tickets_tenant_id_admission_ticket_id",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_recovery_delivery_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_capabilities_expiry",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "expires_at", "consumed_at", "rotated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_capabilities_request",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "recovery_request_id", "purpose" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_active",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "admission_ticket_id", "purpose", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_digest",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_generation",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "admission_ticket_id", "purpose", "capability_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_capabilities_locator",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "locator_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_recovery_delivery_intents_pending",
                table: "ie_admission_recovery_delivery_intents",
                columns: new[] { "handoff_completed_at", "routed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_recovery_delivery_intents_tenant_id_admission_ticket_id",
                table: "ie_admission_recovery_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_recovery_delivery_intents_generation",
                table: "ie_admission_recovery_delivery_intents",
                columns: new[] { "tenant_id", "recovery_request_id", "admission_ticket_id", "purpose", "capability_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_admission_recovery_capabilities");

            migrationBuilder.DropTable(
                name: "ie_admission_recovery_delivery_intents");
        }
    }
}
