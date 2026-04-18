using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecretBindingsAndDataProtectionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "secret_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    infisical_environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    infisical_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    infisical_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    environment_variable_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    inline_ciphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    inline_ciphertext_version = table.Column<int>(type: "integer", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_validation_result = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_validation_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_validated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_secret_bindings", x => x.id);
                    table.CheckConstraint("ck_secret_bindings_scope_scope_id", "(scope = 0 AND scope_id IS NULL) OR (scope = 1 AND scope_id IS NOT NULL)");
                    table.CheckConstraint("ck_secret_bindings_source_metadata", "(source_type = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (source_type = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (source_type = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_scope_scope_id",
                table: "secret_bindings",
                columns: new[] { "scope", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_setting_key_instance_unique",
                table: "secret_bindings",
                column: "setting_key",
                unique: true,
                filter: "scope_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_secret_bindings_setting_key_scope_id_tenant_unique",
                table: "secret_bindings",
                columns: new[] { "setting_key", "scope_id" },
                unique: true,
                filter: "scope_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "secret_bindings");
        }
    }
}
