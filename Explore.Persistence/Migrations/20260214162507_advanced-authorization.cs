using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class advancedauthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_organization_roles_organization_role_id",
                table: "organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_users_user_roles_user_role_id",
                table: "tenant_users");

            migrationBuilder.DropTable(
                name: "organization_roles");

            migrationBuilder.DropTable(
                name: "TenantAdministrators");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "TenantAdministratorRoles");

            migrationBuilder.RenameColumn(
                name: "user_role_id",
                table: "tenant_users",
                newName: "role_id");

            migrationBuilder.RenameIndex(
                name: "ix_tenant_users_user_role_id",
                table: "tenant_users",
                newName: "ix_tenant_users_role_id");

            migrationBuilder.RenameColumn(
                name: "organization_role_id",
                table: "organization_members",
                newName: "role_id");

            migrationBuilder.RenameIndex(
                name: "ix_organization_members_organization_role_id",
                table: "organization_members",
                newName: "ix_organization_members_role_id");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    resource_kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    field_scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    master_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_filtered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_members_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_members_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tenant_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_mastercode",
                table: "permissions",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_resource_action",
                table: "permissions",
                columns: new[] { "resource_kind", "action" });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_scope",
                table: "permissions",
                column: "scope");

            migrationBuilder.CreateIndex(
                name: "ix_rolepermissions_permission",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_rolepermissions_role",
                table: "role_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_mastercode",
                table: "roles",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_scope",
                table: "roles",
                column: "scope");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_members_role_id",
                table: "tenant_members",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_members_tenant_id",
                table: "tenant_members",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_members_user_id",
                table: "tenant_members",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_roles_role_id",
                table: "organization_members",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_users_roles_role_id",
                table: "tenant_users",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_roles_role_id",
                table: "organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_users_roles_role_id",
                table: "tenant_users");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "tenant_members");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "tenant_users",
                newName: "user_role_id");

            migrationBuilder.RenameIndex(
                name: "ix_tenant_users_role_id",
                table: "tenant_users",
                newName: "ix_tenant_users_user_role_id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "organization_members",
                newName: "organization_role_id");

            migrationBuilder.RenameIndex(
                name: "ix_organization_members_role_id",
                table: "organization_members",
                newName: "ix_organization_members_organization_role_id");

            migrationBuilder.CreateTable(
                name: "organization_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAdministratorRoles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_administrator_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    master_code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantAdministrators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_administrator_role_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "ix_user_roles_tenant_id",
                table: "user_roles",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_organization_roles_organization_role_id",
                table: "organization_members",
                column: "organization_role_id",
                principalTable: "organization_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_users_user_roles_user_role_id",
                table: "tenant_users",
                column: "user_role_id",
                principalTable: "user_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
