using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferenceMatrixFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_preference_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    default_email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    default_in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preference_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preference_channels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preference_channels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preference_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_muted = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preference_profiles", x => x.id);
                    table.CheckConstraint("ck_notification_preference_profiles_scope_target", "(scope_id IN (0, 1, 2) AND user_id IS NULL AND organization_id IS NULL AND group_id IS NULL) OR (scope_id = 3 AND organization_id IS NOT NULL AND user_id IS NULL AND group_id IS NULL) OR (scope_id = 4 AND group_id IS NOT NULL AND user_id IS NULL AND organization_id IS NULL) OR (scope_id = 5 AND user_id IS NOT NULL AND organization_id IS NULL AND group_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_notification_preference_profiles_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_preference_profiles_organizations_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_preference_profiles_setting_scopes_scope_id",
                        column: x => x.scope_id,
                        principalTable: "setting_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_preference_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_preference_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_channel_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_channel_preferences", x => x.id);
                    table.CheckConstraint("ck_notification_channel_preferences_scope_target", "(scope_id IN (0, 1, 2) AND user_id IS NULL AND organization_id IS NULL AND group_id IS NULL) OR (scope_id = 3 AND organization_id IS NOT NULL AND user_id IS NULL AND group_id IS NULL) OR (scope_id = 4 AND group_id IS NOT NULL AND user_id IS NULL AND organization_id IS NULL) OR (scope_id = 5 AND user_id IS NOT NULL AND organization_id IS NULL AND group_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_notification_preference_ca",
                        column: x => x.category_id,
                        principalTable: "notification_preference_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_notification_preference_ch",
                        column: x => x.channel_id,
                        principalTable: "notification_preference_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_organizations_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_setting_scopes_scope_id",
                        column: x => x.scope_id,
                        principalTable: "setting_scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_category_id",
                table: "notification_channel_preferences",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_channel_id",
                table: "notification_channel_preferences",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_group_id",
                table: "notification_channel_preferences",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_organization_id",
                table: "notification_channel_preferences",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_resolver",
                table: "notification_channel_preferences",
                columns: new[] { "tenant_id", "category_id", "channel_id", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_scope_id",
                table: "notification_channel_preferences",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_user_id",
                table: "notification_channel_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_channel_preferences_group",
                table: "notification_channel_preferences",
                columns: new[] { "tenant_id", "scope_id", "group_id", "category_id", "channel_id" },
                unique: true,
                filter: "is_deleted = false AND group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notification_channel_preferences_organization",
                table: "notification_channel_preferences",
                columns: new[] { "tenant_id", "scope_id", "organization_id", "category_id", "channel_id" },
                unique: true,
                filter: "is_deleted = false AND organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notification_channel_preferences_scope_default",
                table: "notification_channel_preferences",
                columns: new[] { "tenant_id", "scope_id", "category_id", "channel_id" },
                unique: true,
                filter: "is_deleted = false AND user_id IS NULL AND organization_id IS NULL AND group_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notification_channel_preferences_user",
                table: "notification_channel_preferences",
                columns: new[] { "tenant_id", "scope_id", "user_id", "category_id", "channel_id" },
                unique: true,
                filter: "is_deleted = false AND user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_categories_master_code",
                table: "notification_preference_categories",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_channels_master_code",
                table: "notification_preference_channels",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_preference_profiles_group_id",
                table: "notification_preference_profiles",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_preference_profiles_organization_id",
                table: "notification_preference_profiles",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_preference_profiles_scope_id",
                table: "notification_preference_profiles",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_preference_profiles_user_id",
                table: "notification_preference_profiles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_profiles_group",
                table: "notification_preference_profiles",
                columns: new[] { "tenant_id", "scope_id", "group_id" },
                unique: true,
                filter: "is_deleted = false AND group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_profiles_organization",
                table: "notification_preference_profiles",
                columns: new[] { "tenant_id", "scope_id", "organization_id" },
                unique: true,
                filter: "is_deleted = false AND organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_profiles_scope_default",
                table: "notification_preference_profiles",
                columns: new[] { "tenant_id", "scope_id" },
                unique: true,
                filter: "is_deleted = false AND user_id IS NULL AND organization_id IS NULL AND group_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preference_profiles_user",
                table: "notification_preference_profiles",
                columns: new[] { "tenant_id", "scope_id", "user_id" },
                unique: true,
                filter: "is_deleted = false AND user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_channel_preferences");

            migrationBuilder.DropTable(
                name: "notification_preference_profiles");

            migrationBuilder.DropTable(
                name: "notification_preference_categories");

            migrationBuilder.DropTable(
                name: "notification_preference_channels");
        }
    }
}
