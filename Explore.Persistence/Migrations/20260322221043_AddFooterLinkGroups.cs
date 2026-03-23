using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFooterLinkGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_footer_link_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_footer_link_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_footer_link_groups_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_footer_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    footer_link_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    open_in_new_tab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_footer_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_footer_links_tenant_footer_link_groups_footer_link_g",
                        column: x => x.footer_link_group_id,
                        principalTable: "tenant_footer_link_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_footer_link_groups_tenant_id",
                table: "tenant_footer_link_groups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_footer_link_groups_tenant_id_order",
                table: "tenant_footer_link_groups",
                columns: new[] { "tenant_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_footer_links_footer_link_group_id_order",
                table: "tenant_footer_links",
                columns: new[] { "footer_link_group_id", "order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_footer_links");

            migrationBuilder.DropTable(
                name: "tenant_footer_link_groups");
        }
    }
}
