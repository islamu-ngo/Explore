using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

/// <inheritdoc />
public partial class FixPendingModelChanges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "tenant_id",
            table: "organization_members",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("018e4e5c-7f00-7000-8000-000000000001"));

        migrationBuilder.UpdateData(
            table: "organization_members",
            keyColumn: "id",
            keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000041"),
            column: "tenant_id",
            value: new Guid("018e4e5c-7f00-7000-8000-000000000001"));

        migrationBuilder.CreateIndex(
            name: "ix_organization_members_tenant_id",
            table: "organization_members",
            column: "tenant_id");

        migrationBuilder.AddForeignKey(
            name: "fk_organization_members_tenants_tenant_id",
            table: "organization_members",
            column: "tenant_id",
            principalTable: "tenants",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_organization_members_tenants_tenant_id",
            table: "organization_members");

        migrationBuilder.DropIndex(
            name: "ix_organization_members_tenant_id",
            table: "organization_members");

        migrationBuilder.DropColumn(
            name: "tenant_id",
            table: "organization_members");
    }
}
