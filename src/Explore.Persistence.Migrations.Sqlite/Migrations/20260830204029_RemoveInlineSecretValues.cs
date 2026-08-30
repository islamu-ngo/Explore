using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInlineSecretValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings");

            migrationBuilder.DropColumn(
                name: "inline_ciphertext",
                table: "ie_secret_bindings");

            migrationBuilder.DropColumn(
                name: "inline_ciphertext_version",
                table: "ie_secret_bindings");

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL) OR (secret_source_type_id = 1 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings");

            migrationBuilder.AddColumn<byte[]>(
                name: "inline_ciphertext",
                table: "ie_secret_bindings",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inline_ciphertext_version",
                table: "ie_secret_bindings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (secret_source_type_id = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (secret_source_type_id = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
        }
    }
}
