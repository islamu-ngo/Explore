using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguredAdministratorBootstrapState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_is_completed",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "is_completed",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "selected_deployment_mode",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ie_instance_bootstrap_states",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "completed_identity_fingerprint",
                table: "ie_instance_bootstrap_states",
                type: "TEXT",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "BINARY");

            migrationBuilder.AddColumn<string>(
                name: "configuration_fingerprint",
                table: "ie_instance_bootstrap_states",
                type: "TEXT",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "BINARY");

            migrationBuilder.AddColumn<int>(
                name: "deployment_mode",
                table: "ie_instance_bootstrap_states",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "generation",
                table: "ie_instance_bootstrap_states",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "mode",
                table: "ie_instance_bootstrap_states",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "provider_kind",
                table: "ie_instance_bootstrap_states",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selector_fingerprint",
                table: "ie_instance_bootstrap_states",
                type: "TEXT",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "BINARY");

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "ie_instance_bootstrap_states",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "superseded_at",
                table: "ie_instance_bootstrap_states",
                type: "TEXT",
                nullable: true);

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
                name: "ix_instance_bootstrap_states_completed_by_user_id",
                table: "ie_instance_bootstrap_states",
                column: "completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_generation",
                table: "ie_instance_bootstrap_states",
                column: "generation",
                unique: true,
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_status_generation",
                table: "ie_instance_bootstrap_states",
                columns: new[] { "status", "generation" },
                descending: new[] { false, true });

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_deployment_mode",
                table: "ie_instance_bootstrap_states",
                sql: "deployment_mode BETWEEN 1 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_generation",
                table: "ie_instance_bootstrap_states",
                sql: "generation > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_lifecycle",
                table: "ie_instance_bootstrap_states",
                sql: "(status = 1 AND superseded_at IS NULL AND completed_at IS NULL AND completed_by_user_id IS NULL AND completed_identity_fingerprint IS NULL) OR (status = 2 AND mode = 2 AND superseded_at IS NOT NULL AND completed_at IS NULL AND completed_by_user_id IS NULL AND completed_identity_fingerprint IS NULL) OR (status = 3 AND superseded_at IS NULL AND completed_at IS NOT NULL AND completed_by_user_id IS NOT NULL AND ((mode = 1 AND completed_identity_fingerprint IS NULL) OR (mode = 2 AND completed_identity_fingerprint IS NOT NULL AND completed_identity_fingerprint = selector_fingerprint)))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_mode",
                table: "ie_instance_bootstrap_states",
                sql: "mode BETWEEN 1 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_mode_evidence",
                table: "ie_instance_bootstrap_states",
                sql: "(mode = 1 AND provider_kind IS NULL AND configuration_fingerprint IS NULL AND selector_fingerprint IS NULL) OR (mode = 2 AND provider_kind IS NOT NULL AND configuration_fingerprint IS NOT NULL AND selector_fingerprint IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_provider_kind",
                table: "ie_instance_bootstrap_states",
                sql: "provider_kind IS NULL OR provider_kind BETWEEN 1 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_status",
                table: "ie_instance_bootstrap_states",
                sql: "status BETWEEN 1 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_terminal_timestamps",
                table: "ie_instance_bootstrap_states",
                sql: "(superseded_at IS NULL OR superseded_at >= created_at) AND (completed_at IS NULL OR completed_at >= created_at)");

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

            migrationBuilder.AddForeignKey(
                name: "fk_instance_bootstrap_states_users_completed_by_user_id",
                table: "ie_instance_bootstrap_states",
                column: "completed_by_user_id",
                principalTable: "ie_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_instance_bootstrap_states_users_completed_by_user_id",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropTable(
                name: "ie_setup_enrollment_issuance_claims");

            migrationBuilder.DropTable(
                name: "ie_setup_secret_binding_operations");

            migrationBuilder.DropTable(
                name: "ie_setup_target_enrollments");

            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_completed_by_user_id",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_generation",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_status_generation",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_deployment_mode",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_generation",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_lifecycle",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_mode",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_mode_evidence",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_provider_kind",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_status",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_terminal_timestamps",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "completed_identity_fingerprint",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "configuration_fingerprint",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "deployment_mode",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "generation",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "provider_kind",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "selector_fingerprint",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "status",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "superseded_at",
                table: "ie_instance_bootstrap_states");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ie_instance_bootstrap_states",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "is_completed",
                table: "ie_instance_bootstrap_states",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "selected_deployment_mode",
                table: "ie_instance_bootstrap_states",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_is_completed",
                table: "ie_instance_bootstrap_states",
                column: "is_completed",
                unique: true,
                filter: "\"is_completed\" = true");
        }
    }
}
