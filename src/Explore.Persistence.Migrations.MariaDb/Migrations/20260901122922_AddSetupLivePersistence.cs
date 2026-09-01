using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddSetupLivePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_setup_target_enrollments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    actor_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    challenge_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "ascii_bin")
                        .Annotation("MySql:CharSet", "ascii"),
                    capability_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "ascii_bin")
                        .Annotation("MySql:CharSet", "ascii"),
                    scope_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "ascii_bin")
                        .Annotation("MySql:CharSet", "ascii"),
                    generation = table.Column<long>(type: "bigint", nullable: false),
                    state = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expired_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_setup_target_enrollments", x => x.id);
                    table.UniqueConstraint("ak_setup_target_enrollments_tenant_id_id_actor_id", x => new { x.tenant_id, x.id, x.actor_id });
                    table.CheckConstraint("ck_setup_target_enrollments_generation", "generation > 0");
                    table.CheckConstraint("ck_setup_target_enrollments_lifecycle", "expires_at > created_at AND ((state = 1 AND revoked_at IS NULL AND expired_at IS NULL) OR (state = 2 AND revoked_at IS NOT NULL AND expired_at IS NULL) OR (state = 3 AND revoked_at IS NULL AND expired_at IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_setup_target_enrollments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_setup_enrollment_issuance_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    actor_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    operation_key = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    enrollment_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    enrollment_generation = table.Column<long>(type: "bigint", nullable: false),
                    request_fingerprint = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "ascii_bin")
                        .Annotation("MySql:CharSet", "ascii"),
                    claimed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_setup_enrollment_issuance_claims", x => x.id);
                    table.CheckConstraint("ck_setup_enrollment_claims_generation", "enrollment_generation > 0");
                    table.ForeignKey(
                        name: "fk_ie_setup_enrollment_issuance_claims_ie_setup_target__90f3a697",
                        columns: x => new { x.tenant_id, x.enrollment_id, x.actor_id },
                        principalTable: "ie_setup_target_enrollments",
                        principalColumns: new[] { "tenant_id", "id", "actor_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_setup_secret_binding_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    actor_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    enrollment_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    enrollment_generation = table.Column<long>(type: "bigint", nullable: false),
                    operation_key = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    binding_key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "ascii_bin")
                        .Annotation("MySql:CharSet", "ascii"),
                    request_fingerprint = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "ascii_bin")
                        .Annotation("MySql:CharSet", "ascii"),
                    commitment_key_version = table.Column<int>(type: "int", nullable: false),
                    secret_value_commitment = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "ascii_bin")
                        .Annotation("MySql:CharSet", "ascii"),
                    state = table.Column<int>(type: "int", nullable: false),
                    outcome = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    settled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_setup_secret_binding_operations", x => x.id);
                    table.CheckConstraint("ck_setup_secret_operations_binding", "binding_key IN ('setup.signing', 'setup.encryption')");
                    table.CheckConstraint("ck_setup_secret_operations_lifecycle", "(state = 1 AND outcome = 1 AND settled_at IS NULL) OR (state = 2 AND outcome = 2 AND settled_at IS NOT NULL) OR (state = 3 AND outcome IN (3, 4, 5, 7) AND settled_at IS NOT NULL) OR (state = 4 AND outcome = 6 AND settled_at IS NOT NULL)");
                    table.CheckConstraint("ck_setup_secret_operations_versions", "enrollment_generation > 0 AND commitment_key_version > 0");
                    table.ForeignKey(
                        name: "fk_ie_setup_secret_binding_operations_ie_setup_target_e_b2a7e667",
                        columns: x => new { x.tenant_id, x.enrollment_id, x.actor_id },
                        principalTable: "ie_setup_target_enrollments",
                        principalColumns: new[] { "tenant_id", "id", "actor_id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_setup_enrollment_issuance_claims_tenant_id_enroll_be964297",
                table: "ie_setup_enrollment_issuance_claims",
                columns: new[] { "tenant_id", "enrollment_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_setup_enrollment_issuance_claims_tenant_id_operation_key",
                table: "ie_setup_enrollment_issuance_claims",
                columns: new[] { "tenant_id", "operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_setup_secret_binding_operations_tenant_id_enrollm_2f6dbbae",
                table: "ie_setup_secret_binding_operations",
                columns: new[] { "tenant_id", "enrollment_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_setup_secret_binding_operations_tenant_id_operation_key",
                table: "ie_setup_secret_binding_operations",
                columns: new[] { "tenant_id", "operation_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_setup_enrollment_issuance_claims");

            migrationBuilder.DropTable(
                name: "ie_setup_secret_binding_operations");

            migrationBuilder.DropTable(
                name: "ie_setup_target_enrollments");
        }
    }
}
