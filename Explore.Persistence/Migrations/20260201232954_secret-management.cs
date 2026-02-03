using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class secretmanagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    encrypted_value = table.Column<string>(type: "text", nullable: false),
                    key_version = table.Column<int>(type: "integer", nullable: false),
                    encrypted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    encrypted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.key);
                    table.CheckConstraint("CK_AppSettings_NoHighValueSecrets", "key NOT LIKE 'Database:%' AND key NOT LIKE 'Security:MasterKey%' AND key NOT LIKE 'ConnectionStrings:%'");
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_category",
                table: "app_settings",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_is_sensitive",
                table: "app_settings",
                column: "is_sensitive");

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_key_version",
                table: "app_settings",
                column: "key_version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");
        }
    }
}
