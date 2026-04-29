using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSessionFeaturedImageColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "featured_image_id",
                table: "event_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_featured_image_id",
                table: "event_sessions",
                column: "featured_image_id");

            migrationBuilder.AddForeignKey(
                name: "fk_event_sessions_storage_objects_featured_image_id",
                table: "event_sessions",
                column: "featured_image_id",
                principalTable: "storage_objects",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_storage_objects_featured_image_id",
                table: "event_sessions");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_featured_image_id",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "featured_image_id",
                table: "event_sessions");
        }
    }
}
