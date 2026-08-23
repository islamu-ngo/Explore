using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContractEventLocationPhysicalReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_sessions",
                sql: "location_id IS NULL OR event_location_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSessionGroup_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_session_groups",
                sql: "location_id IS NULL OR event_location_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSessionAgendaItem_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_session_agenda_items",
                sql: "location_id IS NULL OR event_location_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAgendaItem_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_agenda_items",
                sql: "location_id IS NULL OR event_location_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSessionGroup_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_session_groups");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSessionAgendaItem_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_session_agenda_items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAgendaItem_PhysicalLocationRequiresEventLocation",
                schema: "islamu_event",
                table: "event_agenda_items");
        }
    }
}
