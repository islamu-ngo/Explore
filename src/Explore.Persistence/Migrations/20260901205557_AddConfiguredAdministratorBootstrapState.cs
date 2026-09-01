using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
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
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "completed_identity_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "C");

            migrationBuilder.AddColumn<string>(
                name: "configuration_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "C");

            migrationBuilder.AddColumn<int>(
                name: "deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "integer",
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
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "provider_kind",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selector_fingerprint",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true,
                collation: "C");

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "superseded_at",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "timestamp with time zone",
                nullable: true);

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
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "is_completed",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "selected_deployment_mode",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_instance_bootstrap_states_is_completed",
                schema: "islamu_event",
                table: "instance_bootstrap_states",
                column: "is_completed",
                unique: true,
                filter: "\"is_completed\" = true");
        }
    }
}
