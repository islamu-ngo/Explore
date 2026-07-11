// ABOUTME: Adds terminal and moderation EventSessionStatus lookup rows.
// ABOUTME: Keeps migrated databases aligned with the runtime lookup seeder and EventSessionStatusEnum.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    public partial class AddEventSessionTerminalStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "event_session_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 9, "COMPLETED", "Completed", "Session has been completed" },
                    { 10, "MODERATED", "Moderated", "Session was hidden by event-level moderation" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "event_session_statuses",
                keyColumn: "id",
                keyValues: new object[] { 9, 10 });
        }
    }
}
