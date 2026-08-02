using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WebhookOwnerTenantContainment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_groups_group_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_organizations_organization_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_group_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_organization_id",
                table: "webhook_consumers");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_organization_tenants_tenant_id_organization_id",
                table: "organization_tenants",
                columns: new[] { "tenant_id", "organization_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_group_tenants_tenant_id_group_id",
                table: "group_tenants",
                columns: new[] { "tenant_id", "group_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_group_tenants_tenant_id_group_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "group_id" },
                principalTable: "group_tenants",
                principalColumns: new[] { "tenant_id", "group_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_organization_tenants_tenant_id_organizati",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "organization_id" },
                principalTable: "organization_tenants",
                principalColumns: new[] { "tenant_id", "organization_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_group_tenants_tenant_id_group_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_organization_tenants_tenant_id_organizati",
                table: "webhook_consumers");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_organization_tenants_tenant_id_organization_id",
                table: "organization_tenants");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_group_tenants_tenant_id_group_id",
                table: "group_tenants");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_group_id",
                table: "webhook_consumers",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_organization_id",
                table: "webhook_consumers",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_groups_group_id",
                table: "webhook_consumers",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_organizations_organization_id",
                table: "webhook_consumers",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
