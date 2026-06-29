// ABOUTME: Adds optimistic concurrency metadata to event-session speaker junction rows.
// ABOUTME: Backfills existing rows with a non-empty deterministic stamp for expected-stamp updates.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSessionSpeakerConcurrencyStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_session_speakers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("01978f42-5759-7d4b-b507-cc3140000003"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_session_speakers");
        }
    }
}
