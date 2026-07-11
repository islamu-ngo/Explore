// ABOUTME: Adds ProvenanceSource and ProvenanceExternalId nullable columns to events for import/backfill tracking.
// ABOUTME: Required by Task 2.7 lifecycle policy import flow to record where imported events came from.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventProvenanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provenance_external_id",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provenance_source",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "provenance_external_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "provenance_source",
                table: "events");
        }
    }
}
