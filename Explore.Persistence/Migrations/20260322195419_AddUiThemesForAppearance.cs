using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUiThemesForAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ui_themes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    theme_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    light_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ui_themes", x => x.id);
                    table.ForeignKey(
                        name: "fk_ui_themes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_is_default",
                table: "ui_themes",
                column: "is_default",
                unique: true,
                filter: "tenant_id IS NULL AND is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_tenant_id_is_default",
                table: "ui_themes",
                columns: new[] { "tenant_id", "is_default" },
                unique: true,
                filter: "tenant_id IS NOT NULL AND is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_tenant_id_theme_key",
                table: "ui_themes",
                columns: new[] { "tenant_id", "theme_key" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ui_themes_theme_key",
                table: "ui_themes",
                column: "theme_key",
                unique: true,
                filter: "tenant_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ui_themes");
        }
    }
}
