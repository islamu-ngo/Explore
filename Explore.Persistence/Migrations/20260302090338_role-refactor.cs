using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class rolerefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "group_position_id",
                table: "group_members",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "group_position",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    master_code = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_position", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_group_members_group_position_id",
                table: "group_members",
                column: "group_position_id");

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_group_position_group_position_id",
                table: "group_members",
                column: "group_position_id",
                principalTable: "group_position",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_members_group_position_group_position_id",
                table: "group_members");

            migrationBuilder.DropTable(
                name: "group_position");

            migrationBuilder.DropIndex(
                name: "ix_group_members_group_position_id",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "group_position_id",
                table: "group_members");
        }
    }
}
