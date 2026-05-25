using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLocalUserState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_by = table.Column<Guid>(type: "uuid", nullable: true),
                    ban_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    removed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    moderation_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_users", x => x.id);
                    table.CheckConstraint("ck_tenant_users_status", "status_id IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_tenant_users_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenant_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name_override = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    contact_email_override = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    locale = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    preferences_json = table.Column<string>(type: "jsonb", nullable: true),
                    consent_json = table.Column<string>(type: "jsonb", nullable: true),
                    admin_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_user_profiles_tenant_users_tenant_user_id",
                        column: x => x.tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_user_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenantuserprofiles_tenant_contact_email",
                table: "tenant_user_profiles",
                columns: new[] { "tenant_id", "contact_email_override" },
                filter: "contact_email_override IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenantuserprofiles_tenant_user",
                table: "tenant_user_profiles",
                column: "tenant_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_users_actor_id",
                table: "tenant_users",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_users_user_id",
                table: "tenant_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenantusers_tenant_actor",
                table: "tenant_users",
                columns: new[] { "tenant_id", "actor_id" },
                unique: true,
                filter: "actor_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenantusers_tenant_user",
                table: "tenant_users",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_user_profiles");

            migrationBuilder.DropTable(
                name: "tenant_users");
        }
    }
}
