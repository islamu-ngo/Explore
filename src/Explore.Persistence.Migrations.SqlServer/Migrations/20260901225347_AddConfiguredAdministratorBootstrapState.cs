using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguredAdministratorBootstrapState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_is_completed",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "is_completed",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "selected_deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "completed_identity_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "nchar(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<string>(
                name: "configuration_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "nchar(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<int>(
                name: "deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "provider_kind",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selector_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "nchar(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "superseded_at",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "setup_target_enrollments",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    challenge_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    capability_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    scope_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    generation = table.Column<long>(type: "bigint", nullable: false),
                    state = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    expired_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setup_target_enrollments", x => x.id);
                    table.UniqueConstraint("ak_setup_target_enrollments_tenant_id_id_actor_id", x => new { x.tenant_id, x.id, x.actor_id });
                    table.CheckConstraint("ck_setup_target_enrollments_generation", "generation > 0");
                    table.CheckConstraint("ck_setup_target_enrollments_lifecycle", "expires_at > created_at AND ((state = 1 AND revoked_at IS NULL AND expired_at IS NULL) OR (state = 2 AND revoked_at IS NOT NULL AND expired_at IS NULL) OR (state = 3 AND revoked_at IS NULL AND expired_at IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_setup_target_enrollments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "setup_enrollment_issuance_claims",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operation_key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enrollment_generation = table.Column<long>(type: "bigint", nullable: false),
                    request_fingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    claimed_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setup_enrollment_issuance_claims", x => x.id);
                    table.CheckConstraint("ck_setup_enrollment_claims_generation", "enrollment_generation > 0");
                    table.ForeignKey(
                        name: "fk_setup_enrollment_issuance_claims_setup_target_enrollments_tenant_id_enrollment_id_actor_id",
                        columns: x => new { x.tenant_id, x.enrollment_id, x.actor_id },
                        principalSchema: "islamu_event",
                        principalTable: "setup_target_enrollments",
                        principalColumns: new[] { "tenant_id", "id", "actor_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "setup_secret_binding_operations",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enrollment_generation = table.Column<long>(type: "bigint", nullable: false),
                    operation_key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    binding_key = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, collation: "Latin1_General_100_BIN2"),
                    request_fingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    commitment_key_version = table.Column<int>(type: "int", nullable: false),
                    secret_value_commitment = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    state = table.Column<int>(type: "int", nullable: false),
                    outcome = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    settled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setup_secret_binding_operations", x => x.id);
                    table.CheckConstraint("ck_setup_secret_operations_binding", "binding_key IN ('setup.signing', 'setup.encryption')");
                    table.CheckConstraint("ck_setup_secret_operations_lifecycle", "(state = 1 AND outcome = 1 AND settled_at IS NULL) OR (state = 2 AND outcome = 2 AND settled_at IS NOT NULL) OR (state = 3 AND outcome IN (3, 4, 5, 7) AND settled_at IS NOT NULL) OR (state = 4 AND outcome = 6 AND settled_at IS NOT NULL)");
                    table.CheckConstraint("ck_setup_secret_operations_versions", "enrollment_generation > 0 AND commitment_key_version > 0");
                    table.ForeignKey(
                        name: "fk_setup_secret_binding_operations_setup_target_enrollments_tenant_id_enrollment_id_actor_id",
                        columns: x => new { x.tenant_id, x.enrollment_id, x.actor_id },
                        principalSchema: "islamu_event",
                        principalTable: "setup_target_enrollments",
                        principalColumns: new[] { "tenant_id", "id", "actor_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_completed_by_user_id",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                column: "completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                column: "generation",
                unique: true,
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_status_generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                columns: new[] { "status", "generation" },
                descending: new[] { false, true });

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "deployment_mode BETWEEN 1 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "generation > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_lifecycle",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "(status = 1 AND superseded_at IS NULL AND completed_at IS NULL AND completed_by_user_id IS NULL AND completed_identity_fingerprint IS NULL) OR (status = 2 AND mode = 2 AND superseded_at IS NOT NULL AND completed_at IS NULL AND completed_by_user_id IS NULL AND completed_identity_fingerprint IS NULL) OR (status = 3 AND superseded_at IS NULL AND completed_at IS NOT NULL AND completed_by_user_id IS NOT NULL AND ((mode = 1 AND completed_identity_fingerprint IS NULL) OR (mode = 2 AND completed_identity_fingerprint IS NOT NULL AND completed_identity_fingerprint = selector_fingerprint)))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "mode BETWEEN 1 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_mode_evidence",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "(mode = 1 AND provider_kind IS NULL AND configuration_fingerprint IS NULL AND selector_fingerprint IS NULL) OR (mode = 2 AND provider_kind IS NOT NULL AND configuration_fingerprint IS NOT NULL AND selector_fingerprint IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_provider_kind",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "provider_kind IS NULL OR provider_kind BETWEEN 1 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_status",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "status BETWEEN 1 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "ck_instance_bootstrap_states_terminal_timestamps",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                sql: "(superseded_at IS NULL OR superseded_at >= created_at) AND (completed_at IS NULL OR completed_at >= created_at)");

            migrationBuilder.CreateIndex(
                name: "ix_setup_enrollment_issuance_claims_tenant_id_enrollment_id_actor_id",
                schema: "islamu_event",
                table: "setup_enrollment_issuance_claims",
                columns: new[] { "tenant_id", "enrollment_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_setup_enrollment_issuance_claims_tenant_id_operation_key",
                schema: "islamu_event",
                table: "setup_enrollment_issuance_claims",
                columns: new[] { "tenant_id", "operation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_setup_secret_binding_operations_tenant_id_enrollment_id_actor_id",
                schema: "islamu_event",
                table: "setup_secret_binding_operations",
                columns: new[] { "tenant_id", "enrollment_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_setup_secret_binding_operations_tenant_id_operation_key",
                schema: "islamu_event",
                table: "setup_secret_binding_operations",
                columns: new[] { "tenant_id", "operation_key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_instance_bootstrap_states_users_completed_by_user_id",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                column: "completed_by_user_id",
                principalSchema: "islamu_event",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_instance_bootstrap_states_users_completed_by_user_id",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropTable(
                name: "setup_enrollment_issuance_claims",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "setup_secret_binding_operations",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "setup_target_enrollments",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_completed_by_user_id",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropIndex(
                name: "ix_instance_bootstrap_states_status_generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_lifecycle",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_mode_evidence",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_provider_kind",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_status",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_instance_bootstrap_states_terminal_timestamps",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "completed_identity_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "configuration_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "generation",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "provider_kind",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "selector_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.DropColumn(
                name: "superseded_at",
                schema: "islamu_event",
                table: "instance_bootstrap_states");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "is_completed",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "selected_deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_is_completed",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                column: "is_completed",
                unique: true,
                filter: "\"is_completed\" = 1");
        }
    }
}
