using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOrganizationMemberRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_organizations_organization_id1",
                table: "organization_members");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_organization_id1",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "organization_id1",
                table: "organization_members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "organization_id1",
                table: "organization_members",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "organization_members",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000041"),
                column: "organization_id1",
                value: null);

            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_id1",
                table: "organization_members",
                column: "organization_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_organizations_organization_id1",
                table: "organization_members",
                column: "organization_id1",
                principalTable: "organizations",
                principalColumn: "id");
        }
    }
}
