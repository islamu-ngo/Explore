using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
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
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recovery_request_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    purpose = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    capability_version = table.Column<int>(type: "int", nullable: false),
                    lookup_key_version = table.Column<int>(type: "int", nullable: false),
                    lookup_digest = table.Column<string>(type: "char(44)", fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    locator_digest = table.Column<string>(type: "char(44)", fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    rotated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    active_uniqueness_slot = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_ie_admission_recovery_capabilities", x => x.id);
                    table.UniqueConstraint("ak_admission_recovery_capabilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_recovery_capabilities_lifecycle", "(consumed_at IS NULL OR rotated_at IS NULL) AND ((consumed_at IS NULL AND rotated_at IS NULL AND active_uniqueness_slot = 0) OR ((consumed_at IS NOT NULL OR rotated_at IS NOT NULL) AND active_uniqueness_slot = capability_version))");
                    table.CheckConstraint("ck_admission_recovery_capabilities_versions", "capability_version > 0 AND lookup_key_version > 0");
                    table.ForeignKey(
                        name: "FK_ie_admission_recovery_capabilities_ie_admission_tick_04518CFA",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_recovery_delivery_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recovery_request_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    purpose = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    capability_version = table.Column<int>(type: "int", nullable: false),
                    protected_material = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    protection_version = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    routed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    handoff_completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    handoff_receipt_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_recovery_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_recovery_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_recovery_delivery_intents_handoff", "(handoff_completed_at IS NULL AND handoff_receipt_id IS NULL) OR (handoff_completed_at IS NOT NULL AND handoff_receipt_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_recovery_delivery_intents_versions", "capability_version > 0 AND protection_version > 0");
                    table.ForeignKey(
                        name: "FK_ie_admission_recovery_delivery_intents_ie_admission__A04C4535",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_capabilities_expires_at_consum_5A1D4199",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "expires_at", "consumed_at", "rotated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_capabilities_tenant_id_admissi_7CFC9EC5",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "admission_ticket_id", "purpose", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_capabilities_tenant_id_admissi_B375B532",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "admission_ticket_id", "purpose", "capability_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_capabilities_tenant_id_lookup__23A06283",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_capabilities_tenant_id_lookup__441FC60D",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "locator_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_capabilities_tenant_id_recover_714F865F",
                table: "ie_admission_recovery_capabilities",
                columns: new[] { "tenant_id", "recovery_request_id", "purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_delivery_intents_handoff_compl_67385637",
                table: "ie_admission_recovery_delivery_intents",
                columns: new[] { "handoff_completed_at", "routed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_delivery_intents_tenant_id_adm_31C40188",
                table: "ie_admission_recovery_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_recovery_delivery_intents_tenant_id_rec_8A76BF6F",
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
