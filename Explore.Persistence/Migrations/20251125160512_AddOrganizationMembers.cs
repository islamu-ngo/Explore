using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "organization_members",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            /*
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "organization_members",
                type: "text",
                nullable: false,
                defaultValue: "");
            */

            /*
            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "organization_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            */

            /*
            migrationBuilder.CreateIndex(
                name: "ix_organization_members_organization_id",
                table: "organization_members",
                column: "organization_id");
            */

            /*
            migrationBuilder.AddForeignKey(
                name: "fk_organization_members_organizations_organization_id",
                table: "organization_members",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_organization_members_organizations_organization_id",
                table: "organization_members");

            migrationBuilder.DropIndex(
                name: "ix_organization_members_organization_id",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "email",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "role",
                table: "organization_members");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "organization_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
