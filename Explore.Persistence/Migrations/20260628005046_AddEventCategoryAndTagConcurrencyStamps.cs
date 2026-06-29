// ABOUTME: Adds optimistic concurrency metadata to event-category and event-tag junction rows.
// ABOUTME: Backfills existing rows with non-empty deterministic stamps for If-Match-ready updates.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCategoryAndTagConcurrencyStamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_tags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("01978f42-5046-7c7c-b0df-cc3140000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "event_categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("01978f42-5046-7c7c-b0df-cc3140000002"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_tags");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "event_categories");
        }
    }
}
