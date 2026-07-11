// ABOUTME: EF migration adding optimistic concurrency to event registration rows.
// ABOUTME: Backfills existing registrations with a non-empty concurrency stamp.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRegistrationConcurrencyStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_registrations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("01978f42-2f00-7b4d-9d7e-b7d5f1a9a005"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_registrations");
        }
    }
}
