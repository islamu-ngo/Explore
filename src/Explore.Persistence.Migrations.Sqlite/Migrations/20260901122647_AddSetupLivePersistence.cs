using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
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
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    challenge_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false, collation: "BINARY"),
                    capability_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false, collation: "BINARY"),
                    scope_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false, collation: "BINARY"),
                    generation = table.Column<long>(type: "INTEGER", nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    expired_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "ie_setup_enrollment_issuance_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_key = table.Column<Guid>(type: "TEXT", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    enrollment_generation = table.Column<long>(type: "INTEGER", nullable: false),
                    request_fingerprint = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false, collation: "BINARY"),
                    claimed_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_setup_enrollment_issuance_claims", x => x.id);
                    table.CheckConstraint("ck_setup_enrollment_claims_generation", "enrollment_generation > 0");
                    table.ForeignKey(
                        name: "fk_setup_enrollment_issuance_claims_setup_target_enrollments_tenant_id_enrollment_id_actor_id",
                        columns: x => new { x.tenant_id, x.enrollment_id, x.actor_id },
                        principalTable: "ie_setup_target_enrollments",
                        principalColumns: new[] { "tenant_id", "id", "actor_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_setup_secret_binding_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    enrollment_generation = table.Column<long>(type: "INTEGER", nullable: false),
                    operation_key = table.Column<Guid>(type: "TEXT", nullable: false),
                    binding_key = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false, collation: "BINARY"),
                    request_fingerprint = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false, collation: "BINARY"),
                    commitment_key_version = table.Column<int>(type: "INTEGER", nullable: false),
                    secret_value_commitment = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false, collation: "BINARY"),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    settled_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_setup_secret_binding_operations", x => x.id);
                    table.CheckConstraint("ck_setup_secret_operations_binding", "binding_key IN ('setup.signing', 'setup.encryption')");
                    table.CheckConstraint("ck_setup_secret_operations_lifecycle", "(state = 1 AND outcome = 1 AND settled_at IS NULL) OR (state = 2 AND outcome = 2 AND settled_at IS NOT NULL) OR (state = 3 AND outcome IN (3, 4, 5, 7) AND settled_at IS NOT NULL) OR (state = 4 AND outcome = 6 AND settled_at IS NOT NULL)");
                    table.CheckConstraint("ck_setup_secret_operations_versions", "enrollment_generation > 0 AND commitment_key_version > 0");
                    table.ForeignKey(
                        name: "fk_setup_secret_binding_operations_setup_target_enrollments_tenant_id_enrollment_id_actor_id",
                        columns: x => new { x.tenant_id, x.enrollment_id, x.actor_id },
                        principalTable: "ie_setup_target_enrollments",
                        principalColumns: new[] { "tenant_id", "id", "actor_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_setup_enrollment_issuance_claims_tenant_id_enrollment_id_actor_id",
                table: "ie_setup_enrollment_issuance_claims",
                columns: new[] { "tenant_id", "enrollment_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_setup_enrollment_issuance_claims_tenant_id_operation_key",
                table: "ie_setup_enrollment_issuance_claims",
                columns: new[] { "tenant_id", "operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_setup_secret_binding_operations_tenant_id_enrollment_id_actor_id",
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
