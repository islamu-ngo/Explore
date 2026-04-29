using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Appearance
{
    /// <inheritdoc />
    public partial class AddAppearancePresetProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dark_primary_contrast_text",
                table: "ui_themes",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "dark_secondary_contrast_text",
                table: "ui_themes",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "light_primary_contrast_text",
                table: "ui_themes",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "light_secondary_contrast_text",
                table: "ui_themes",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ui_theme_presets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    theme_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    light_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
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
                    dark_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
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
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_editable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    seed_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deprecated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ui_theme_presets", x => x.id);
                    table.ForeignKey(
                        name: "fk_ui_theme_presets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_appearance_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    theme_mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    light_snapshot_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_snapshot_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    light_snapshot_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    light_snapshot_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_snapshot_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_primary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_secondary_contrast_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_background = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_surface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_appbar_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_snapshot_appbar_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_drawer_background = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dark_snapshot_drawer_text = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_drawer_icon = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_text_primary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_text_secondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_info = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_success = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_warning = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_error = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_lines_default = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    dark_snapshot_divider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_preset_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    source_preset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_preset_seed_version = table.Column<int>(type: "integer", nullable: true),
                    is_user_editable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cloned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_appearance_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_appearance_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    active_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    theme_mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "System"),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "auto"),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_appearance_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_appearance_preferences_user_appearance_profiles_active",
                        column: x => x.active_profile_id,
                        principalTable: "user_appearance_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ui_theme_presets_tenant_id_is_active",
                table: "ui_theme_presets",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_ui_theme_presets_tenant_id_theme_key",
                table: "ui_theme_presets",
                columns: new[] { "tenant_id", "theme_key" },
                unique: true,
                filter: "tenant_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ui_theme_presets_theme_key",
                table: "ui_theme_presets",
                column: "theme_key",
                unique: true,
                filter: "tenant_id IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_preferences_active_profile_id",
                table: "user_appearance_preferences",
                column: "active_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_preferences_user_id_tenant_id",
                table: "user_appearance_preferences",
                columns: new[] { "user_id", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_source_preset_id",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "source_preset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_tenant_id_is_archived",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "tenant_id", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_tenant_id_is_default",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "tenant_id", "is_default" },
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "ix_user_appearance_profiles_user_id_tenant_id_name",
                table: "user_appearance_profiles",
                columns: new[] { "user_id", "tenant_id", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ui_theme_presets");

            migrationBuilder.DropTable(
                name: "user_appearance_preferences");

            migrationBuilder.DropTable(
                name: "user_appearance_profiles");

            migrationBuilder.DropColumn(
                name: "dark_primary_contrast_text",
                table: "ui_themes");

            migrationBuilder.DropColumn(
                name: "dark_secondary_contrast_text",
                table: "ui_themes");

            migrationBuilder.DropColumn(
                name: "light_primary_contrast_text",
                table: "ui_themes");

            migrationBuilder.DropColumn(
                name: "light_secondary_contrast_text",
                table: "ui_themes");
        }
    }
}
