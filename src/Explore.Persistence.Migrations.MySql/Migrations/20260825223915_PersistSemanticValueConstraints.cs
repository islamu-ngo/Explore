using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class PersistSemanticValueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_LocationPii_CoordinateShape",
                table: "ie_location_pii",
                sql: "(latitude IS NULL AND longitude IS NULL)\nOR (latitude IS NOT NULL AND longitude IS NOT NULL\n    AND latitude BETWEEN -90 AND 90\n    AND longitude BETWEEN -180 AND 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventTicketType_MoneyNonnegative",
                table: "ie_event_ticket_types",
                sql: "(fixed_price_minor IS NULL OR fixed_price_minor >= 0)\nAND (minimum_price_minor IS NULL OR minimum_price_minor >= 0)\nAND (suggested_price_minor IS NULL OR suggested_price_minor >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalDateRange",
                table: "ie_event_sessions",
                sql: "local_end_date IS NULL OR local_start_date IS NULL OR local_end_date >= local_start_date");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAgendaItem_LocalDateRange",
                table: "ie_event_agenda_items",
                sql: "local_end_date >= local_start_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LocationPii_CoordinateShape",
                table: "ie_location_pii");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventTicketType_MoneyNonnegative",
                table: "ie_event_ticket_types");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalDateRange",
                table: "ie_event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAgendaItem_LocalDateRange",
                table: "ie_event_agenda_items");
        }
    }
}
