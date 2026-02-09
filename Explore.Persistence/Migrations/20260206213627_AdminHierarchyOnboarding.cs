using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

/// <inheritdoc />
public partial class AdminHierarchyOnboarding : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings");

        migrationBuilder.RenameColumn(
            name: "key",
            table: "tenant_setting_overrides",
            newName: "setting_key");

        migrationBuilder.RenameIndex(
            name: "ix_tenant_setting_overrides_tenant_id_key",
            table: "tenant_setting_overrides",
            newName: "ix_tenant_setting_overrides_tenant_id_setting_key");

        migrationBuilder.RenameColumn(
            name: "key",
            table: "system_settings",
            newName: "setting_key");

        migrationBuilder.RenameIndex(
            name: "ix_system_settings_key",
            table: "system_settings",
            newName: "ix_system_settings_setting_key");

        migrationBuilder.RenameColumn(
            name: "key",
            table: "ModuleDefinitions",
            newName: "module_key");

        migrationBuilder.RenameIndex(
            name: "ix_module_definitions_key",
            table: "ModuleDefinitions",
            newName: "ix_module_definitions_module_key");

        migrationBuilder.RenameColumn(
            name: "key",
            table: "app_settings",
            newName: "config_key");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "users",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "user_external_logins",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "user_authentication_tokens",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tenants",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tenant_settings",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tenant_setting_overrides",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tags",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "organizations",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "locations",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "events",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AddColumn<bool>(
            name: "is_user_reported",
            table: "events",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "categories",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "actors",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "uuidv7()");

        migrationBuilder.CreateTable(
            name: "InstanceAdministrators",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                granted_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_instance_administrators", x => x.id);
                table.ForeignKey(
                    name: "fk_instance_administrators_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "InstanceBootstrapStates",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                selected_deployment_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_instance_bootstrap_states", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "TenantAdministratorRoles",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_administrator_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "TenantOnboardingStates",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_onboarding_states", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_onboarding_states_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TenantAdministrators",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_administrator_role_id = table.Column<int>(type: "integer", nullable: false),
                granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                granted_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_administrators", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_administrators_tenant_administrator_roles_tenant_admi",
                    column: x => x.tenant_administrator_role_id,
                    principalTable: "TenantAdministratorRoles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tenant_administrators_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_tenant_administrators_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.AddCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings",
            sql: "config_key NOT LIKE 'Database:%' AND config_key NOT LIKE 'Security:MasterKey%' AND config_key NOT LIKE 'ConnectionStrings:%'");

        migrationBuilder.CreateIndex(
            name: "ix_instance_administrators_user_id",
            table: "InstanceAdministrators",
            column: "user_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tenant_administrator_roles_master_code",
            table: "TenantAdministratorRoles",
            column: "master_code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tenant_administrators_tenant_administrator_role_id",
            table: "TenantAdministrators",
            column: "tenant_administrator_role_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_administrators_tenant_id_user_id",
            table: "TenantAdministrators",
            columns: new[] { "tenant_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tenant_administrators_user_id",
            table: "TenantAdministrators",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_onboarding_states_tenant_id",
            table: "TenantOnboardingStates",
            column: "tenant_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InstanceAdministrators");

        migrationBuilder.DropTable(
            name: "InstanceBootstrapStates");

        migrationBuilder.DropTable(
            name: "TenantAdministrators");

        migrationBuilder.DropTable(
            name: "TenantOnboardingStates");

        migrationBuilder.DropTable(
            name: "TenantAdministratorRoles");

        migrationBuilder.DropCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings");

        migrationBuilder.DropColumn(
            name: "is_user_reported",
            table: "events");

        migrationBuilder.RenameColumn(
            name: "setting_key",
            table: "tenant_setting_overrides",
            newName: "key");

        migrationBuilder.RenameIndex(
            name: "ix_tenant_setting_overrides_tenant_id_setting_key",
            table: "tenant_setting_overrides",
            newName: "ix_tenant_setting_overrides_tenant_id_key");

        migrationBuilder.RenameColumn(
            name: "setting_key",
            table: "system_settings",
            newName: "key");

        migrationBuilder.RenameIndex(
            name: "ix_system_settings_setting_key",
            table: "system_settings",
            newName: "ix_system_settings_key");

        migrationBuilder.RenameColumn(
            name: "module_key",
            table: "ModuleDefinitions",
            newName: "key");

        migrationBuilder.RenameIndex(
            name: "ix_module_definitions_module_key",
            table: "ModuleDefinitions",
            newName: "ix_module_definitions_key");

        migrationBuilder.RenameColumn(
            name: "config_key",
            table: "app_settings",
            newName: "key");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "users",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "user_external_logins",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "user_authentication_tokens",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tenants",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tenant_settings",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tenant_setting_overrides",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "tags",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "organizations",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "locations",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "events",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "categories",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "id",
            table: "actors",
            type: "uuid",
            nullable: false,
            defaultValueSql: "uuidv7()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AppSettings_NoHighValueSecrets",
            table: "app_settings",
            sql: "key NOT LIKE 'Database:%' AND key NOT LIKE 'Security:MasterKey%' AND key NOT LIKE 'ConnectionStrings:%'");
    }
}
