// ABOUTME: EF migration adding optimistic concurrency and audit metadata to locations.
// ABOUTME: Backfills a non-empty concurrency stamp and timestamp default for existing rows.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAuditAndConcurrencyStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "locations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("01978f42-2f00-7b4d-9d7e-b7d5f1a9a004"));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "locations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "locations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "locations");
        }
    }
}
