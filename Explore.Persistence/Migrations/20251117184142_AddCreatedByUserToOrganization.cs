using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserToOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "program_type_id",
                table: "programs",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by_user_id",
                table: "organizations",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "event_types",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.InsertData(
                table: "education_types",
                columns: new[] { "id", "description", "full_name" },
                values: new object[,]
                {
                    { 1, null, "School" },
                    { 2, null, "Institut" },
                    { 3, null, "Course" }
                });

            migrationBuilder.UpdateData(
                table: "organizations",
                keyColumn: "id",
                keyValue: new Guid("018e4e5c-7f00-7000-8000-000000000001"),
                columns: new[] { "created_at", "created_by_user_id" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            // Delete programs that reference non-existent organizations before adding the foreign key
            migrationBuilder.Sql(@"
                DELETE FROM programs 
                WHERE organization_id NOT IN (SELECT id FROM organizations);
            ");

            migrationBuilder.CreateIndex(
                name: "ix_programs_organization_id",
                table: "programs",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "fk_programs_organizations_organization_id",
                table: "programs",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_programs_organizations_organization_id",
                table: "programs");

            migrationBuilder.DropIndex(
                name: "ix_programs_organization_id",
                table: "programs");

            migrationBuilder.DeleteData(
                table: "education_types",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "education_types",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "education_types",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "organizations");

            migrationBuilder.AlterColumn<int>(
                name: "program_type_id",
                table: "programs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "event_types",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
