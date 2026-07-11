// ABOUTME: Adds optimistic concurrency metadata to event-session language junction rows.
// ABOUTME: Backfills existing rows with a non-empty deterministic stamp for If-Match PATCH updates.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSessionLanguageConcurrencyStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_languages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("01978f42-3400-7b4d-9d7e-b7d5f1a9a006"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_languages");
        }
    }
}
