using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantSettingsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_public_group_creation = table.Column<bool>(type: "boolean", nullable: false),
                    allow_public_organization_registration = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    event_publishing_policy = table.Column<int>(type: "integer", nullable: false),
                    require_group_approval = table.Column<bool>(type: "boolean", nullable: false),
                    require_organization_verification = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_settings_groups_default_group_id",
                        column: x => x.default_group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tenant_settings_organizations_default_organization_id",
                        column: x => x.default_organization_id,
                        principalTable: "organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tenant_settings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_default_group_id",
                table: "tenant_settings",
                column: "default_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_default_organization_id",
                table: "tenant_settings",
                column: "default_organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_tenant_id",
                table: "tenant_settings",
                column: "tenant_id");
        }
    }
}
