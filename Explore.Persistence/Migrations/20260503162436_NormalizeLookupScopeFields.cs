using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLookupScopeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notifications_actor_types_notification_scope_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_secret_bindings_scope_scope_id",
                table: "secret_bindings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_scope_scope_id",
                table: "secret_bindings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "secret_bindings");

            migrationBuilder.RenameColumn(
                name: "value_type",
                table: "system_settings",
                newName: "setting_value_type_id");

            migrationBuilder.RenameColumn(
                name: "source_type",
                table: "secret_bindings",
                newName: "secret_source_type_id");

            migrationBuilder.RenameColumn(
                name: "scope",
                table: "secret_bindings",
                newName: "setting_scope_id");

            migrationBuilder.RenameColumn(
                name: "last_validation_result",
                table: "secret_bindings",
                newName: "secret_validation_status_id");

            migrationBuilder.RenameColumn(
                name: "scope",
                table: "roles",
                newName: "role_scope_id");

            migrationBuilder.RenameIndex(
                name: "ix_roles_scope",
                table: "roles",
                newName: "ix_roles_role_scope_id");

            migrationBuilder.RenameColumn(
                name: "scope",
                table: "permissions",
                newName: "role_scope_id");

            migrationBuilder.RenameIndex(
                name: "ix_permissions_scope",
                table: "permissions",
                newName: "ix_permissions_role_scope_id");

            migrationBuilder.RenameColumn(
                name: "owner_type",
                table: "external_api_keys",
                newName: "external_api_key_owner_type_id");

            migrationBuilder.RenameIndex(
                name: "ix_external_api_keys_tenant_id_owner_type_owner_id",
                table: "external_api_keys",
                newName: "ix_external_api_keys_tenant_id_external_api_key_owner_type_id_");

            migrationBuilder.RenameColumn(
                name: "scope",
                table: "configuration_change_logs",
                newName: "setting_scope_id");

            migrationBuilder.RenameIndex(
                name: "ix_configuration_change_logs_scope_scope_id",
                table: "configuration_change_logs",
                newName: "ix_configuration_change_logs_setting_scope_id_scope_id");

            migrationBuilder.CreateTable(
                name: "external_api_key_owner_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_owner_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_scope_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_scope_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "secret_source_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_secret_source_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "secret_validation_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_secret_validation_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "setting_scopes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setting_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "setting_value_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setting_value_types", x => x.id);
                });

            migrationBuilder.Sql("UPDATE secret_bindings SET setting_scope_id = setting_scope_id + 1 WHERE setting_scope_id IN (0, 1);");

            migrationBuilder.InsertData(
                table: "external_api_key_owner_types",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "USER", "User", "External API key owned by a user" },
                    { 2, "ORGANIZATION", "Organization", "External API key owned by an organization" },
                    { 3, "GROUP", "Group", "External API key owned by a group" },
                    { 4, "TENANT", "Tenant", "External API key owned by a tenant" },
                    { 5, "INSTANCE_ADMIN", "Instance Admin", "External API key owned by an instance administrator" }
                });

            migrationBuilder.InsertData(
                table: "notification_scope_types",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "USER", "User", "Notification targets a single user" },
                    { 2, "ORGANIZATION", "Organization", "Notification targets an organization scope" },
                    { 4, "GROUP", "Group", "Notification targets a group scope" },
                    { 5, "SYSTEM", "System", "Notification targets a system scope" }
                });

            migrationBuilder.InsertData(
                table: "role_scopes",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 0, "PLATFORM", "Platform", "Platform-wide roles and permissions" },
                    { 1, "TENANT", "Tenant", "Tenant-scoped roles and permissions" },
                    { 2, "ORGANIZATION", "Organization", "Organization-scoped roles and permissions" },
                    { 3, "GROUP", "Group", "Group-scoped roles and permissions" },
                    { 4, "EVENT", "Event", "Event-scoped roles and permissions" }
                });

            migrationBuilder.InsertData(
                table: "secret_source_types",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 0, "INFISICAL", "Infisical", "Secret value is stored in Infisical" },
                    { 1, "INLINE_ENCRYPTED", "Inline Encrypted", "Secret value is stored encrypted in the database" },
                    { 2, "ENVIRONMENT_VARIABLE", "Environment Variable", "Secret value is resolved from an environment variable" }
                });

            migrationBuilder.InsertData(
                table: "secret_validation_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 0, "NOT_VALIDATED", "Not Validated", "Secret source has not been validated" },
                    { 1, "SUCCESS", "Success", "Secret source validation succeeded" },
                    { 2, "FAILURE", "Failure", "Secret source validation failed" }
                });

            migrationBuilder.InsertData(
                table: "setting_scopes",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 0, "SYSTEM", "System", "Global system configuration scope" },
                    { 1, "INSTANCE", "Instance", "Application instance configuration scope" },
                    { 2, "TENANT", "Tenant", "Tenant configuration scope" },
                    { 3, "ORGANIZATION", "Organization", "Organization configuration scope" },
                    { 4, "GROUP", "Group", "Group configuration scope" },
                    { 5, "USER", "User", "User configuration scope" }
                });

            migrationBuilder.InsertData(
                table: "setting_value_types",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 0, "STRING", "String", "String setting value" },
                    { 1, "INTEGER", "Integer", "Integer setting value" },
                    { 2, "BOOLEAN", "Boolean", "Boolean setting value" },
                    { 3, "DECIMAL", "Decimal", "Decimal setting value" },
                    { 4, "JSON", "JSON", "JSON setting value" },
                    { 5, "DATE_TIME", "Date/Time", "Date/time setting value" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_setting_value_type_id",
                table: "system_settings",
                column: "setting_value_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_secret_source_type_id",
                table: "secret_bindings",
                column: "secret_source_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_secret_validation_status_id",
                table: "secret_bindings",
                column: "secret_validation_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_setting_scope_id_scope_id",
                table: "secret_bindings",
                columns: new[] { "setting_scope_id", "scope_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_setting_scope_scope_id",
                table: "secret_bindings",
                sql: "(setting_scope_id = 1 AND scope_id IS NULL) OR (setting_scope_id = 2 AND scope_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (secret_source_type_id = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (secret_source_type_id = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_external_api_key_owner_type_id",
                table: "external_api_keys",
                column: "external_api_key_owner_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_key_owner_types_master_code",
                table: "external_api_key_owner_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_scope_types_master_code",
                table: "notification_scope_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_scopes_master_code",
                table: "role_scopes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_secret_source_types_master_code",
                table: "secret_source_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_secret_validation_statuses_master_code",
                table: "secret_validation_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_setting_scopes_master_code",
                table: "setting_scopes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_setting_value_types_master_code",
                table: "setting_value_types",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_configuration_change_logs_setting_scopes_setting_scope_id",
                table: "configuration_change_logs",
                column: "setting_scope_id",
                principalTable: "setting_scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_external_api_keys_external_api_key_owner_types_external_api",
                table: "external_api_keys",
                column: "external_api_key_owner_type_id",
                principalTable: "external_api_key_owner_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_notification_scope_types_notification_scope_id",
                table: "notifications",
                column: "notification_scope_id",
                principalTable: "notification_scope_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_permissions_role_scopes_role_scope_id",
                table: "permissions",
                column: "role_scope_id",
                principalTable: "role_scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_roles_role_scopes_role_scope_id",
                table: "roles",
                column: "role_scope_id",
                principalTable: "role_scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_secret_bindings_secret_source_types_secret_source_type_id",
                table: "secret_bindings",
                column: "secret_source_type_id",
                principalTable: "secret_source_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_secret_bindings_secret_validation_statuses_secret_validatio",
                table: "secret_bindings",
                column: "secret_validation_status_id",
                principalTable: "secret_validation_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_secret_bindings_setting_scopes_setting_scope_id",
                table: "secret_bindings",
                column: "setting_scope_id",
                principalTable: "setting_scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_system_settings_setting_value_types_setting_value_type_id",
                table: "system_settings",
                column: "setting_value_type_id",
                principalTable: "setting_value_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_configuration_change_logs_setting_scopes_setting_scope_id",
                table: "configuration_change_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_external_api_keys_external_api_key_owner_types_external_api",
                table: "external_api_keys");

            migrationBuilder.DropForeignKey(
                name: "fk_notifications_notification_scope_types_notification_scope_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "fk_permissions_role_scopes_role_scope_id",
                table: "permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_roles_role_scopes_role_scope_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "fk_secret_bindings_secret_source_types_secret_source_type_id",
                table: "secret_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_secret_bindings_secret_validation_statuses_secret_validatio",
                table: "secret_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_secret_bindings_setting_scopes_setting_scope_id",
                table: "secret_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_system_settings_setting_value_types_setting_value_type_id",
                table: "system_settings");

            migrationBuilder.DropTable(
                name: "external_api_key_owner_types");

            migrationBuilder.DropTable(
                name: "notification_scope_types");

            migrationBuilder.DropTable(
                name: "role_scopes");

            migrationBuilder.DropTable(
                name: "secret_source_types");

            migrationBuilder.DropTable(
                name: "secret_validation_statuses");

            migrationBuilder.DropTable(
                name: "setting_scopes");

            migrationBuilder.DropTable(
                name: "setting_value_types");

            migrationBuilder.DropIndex(
                name: "ix_system_settings_setting_value_type_id",
                table: "system_settings");

            migrationBuilder.DropIndex(
                name: "ix_secret_bindings_secret_source_type_id",
                table: "secret_bindings");

            migrationBuilder.DropIndex(
                name: "ix_secret_bindings_secret_validation_status_id",
                table: "secret_bindings");

            migrationBuilder.DropIndex(
                name: "ix_secret_bindings_setting_scope_id_scope_id",
                table: "secret_bindings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_setting_scope_scope_id",
                table: "secret_bindings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "secret_bindings");

            migrationBuilder.DropIndex(
                name: "ix_external_api_keys_external_api_key_owner_type_id",
                table: "external_api_keys");

            migrationBuilder.Sql("UPDATE secret_bindings SET setting_scope_id = setting_scope_id - 1 WHERE setting_scope_id IN (1, 2);");

            migrationBuilder.RenameColumn(
                name: "setting_value_type_id",
                table: "system_settings",
                newName: "value_type");

            migrationBuilder.RenameColumn(
                name: "setting_scope_id",
                table: "secret_bindings",
                newName: "scope");

            migrationBuilder.RenameColumn(
                name: "secret_validation_status_id",
                table: "secret_bindings",
                newName: "last_validation_result");

            migrationBuilder.RenameColumn(
                name: "secret_source_type_id",
                table: "secret_bindings",
                newName: "source_type");

            migrationBuilder.RenameColumn(
                name: "role_scope_id",
                table: "roles",
                newName: "scope");

            migrationBuilder.RenameIndex(
                name: "ix_roles_role_scope_id",
                table: "roles",
                newName: "ix_roles_scope");

            migrationBuilder.RenameColumn(
                name: "role_scope_id",
                table: "permissions",
                newName: "scope");

            migrationBuilder.RenameIndex(
                name: "ix_permissions_role_scope_id",
                table: "permissions",
                newName: "ix_permissions_scope");

            migrationBuilder.RenameColumn(
                name: "external_api_key_owner_type_id",
                table: "external_api_keys",
                newName: "owner_type");

            migrationBuilder.RenameIndex(
                name: "ix_external_api_keys_tenant_id_external_api_key_owner_type_id_",
                table: "external_api_keys",
                newName: "ix_external_api_keys_tenant_id_owner_type_owner_id");

            migrationBuilder.RenameColumn(
                name: "setting_scope_id",
                table: "configuration_change_logs",
                newName: "scope");

            migrationBuilder.RenameIndex(
                name: "ix_configuration_change_logs_setting_scope_id_scope_id",
                table: "configuration_change_logs",
                newName: "ix_configuration_change_logs_scope_scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_scope_scope_id",
                table: "secret_bindings",
                columns: new[] { "scope", "scope_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_scope_scope_id",
                table: "secret_bindings",
                sql: "(scope = 0 AND scope_id IS NULL) OR (scope = 1 AND scope_id IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "secret_bindings",
                sql: "(source_type = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (source_type = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (source_type = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_actor_types_notification_scope_id",
                table: "notifications",
                column: "notification_scope_id",
                principalTable: "actor_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
