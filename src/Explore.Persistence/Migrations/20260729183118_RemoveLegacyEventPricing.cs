using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyEventPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Event_NonNegativePrice",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_NonNegativePrice",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "events");

            migrationBuilder.DropColumn(
                name: "price",
                table: "events");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "price",
                table: "event_sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "events",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "events",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "event_sessions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "event_sessions",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Event_NonNegativePrice",
                table: "events",
                sql: "price IS NULL OR price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_NonNegativePrice",
                table: "event_sessions",
                sql: "price IS NULL OR price >= 0");
        }
    }
}
