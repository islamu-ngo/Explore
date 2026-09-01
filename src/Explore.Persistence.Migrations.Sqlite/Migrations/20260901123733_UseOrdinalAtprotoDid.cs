using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UseOrdinalAtprotoDid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "did",
                table: "ie_atproto_identities",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                collation: "BINARY",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2048);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "did",
                table: "ie_atproto_identities",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2048,
                oldCollation: "BINARY");
        }
    }
}
