using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupHierarchyParents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_group_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_organization_id",
                table: "groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_organizations_tenant_id_id",
                table: "organizations",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_groups_tenant_id_id",
                table: "groups",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_parent_group",
                table: "groups",
                columns: new[] { "tenant_id", "parent_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_tenant_parent_organization",
                table: "groups",
                columns: new[] { "tenant_id", "parent_organization_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_no_self_parent",
                table: "groups",
                sql: "parent_group_id IS NULL OR parent_group_id <> id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_groups_parent_exclusive",
                table: "groups",
                sql: "parent_organization_id IS NULL OR parent_group_id IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_groups_groups_tenant_id_parent_group_id",
                table: "groups",
                columns: new[] { "tenant_id", "parent_group_id" },
                principalTable: "groups",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_groups_organizations_tenant_id_parent_organization_id",
                table: "groups",
                columns: new[] { "tenant_id", "parent_organization_id" },
                principalTable: "organizations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_groups_groups_tenant_id_parent_group_id",
                table: "groups");

            migrationBuilder.DropForeignKey(
                name: "fk_groups_organizations_tenant_id_parent_organization_id",
                table: "groups");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_organizations_tenant_id_id",
                table: "organizations");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_groups_tenant_id_id",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_tenant_parent_group",
                table: "groups");

            migrationBuilder.DropIndex(
                name: "ix_groups_tenant_parent_organization",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_no_self_parent",
                table: "groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_groups_parent_exclusive",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "parent_group_id",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "parent_organization_id",
                table: "groups");
        }
    }
}
