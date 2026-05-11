using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSessionKindToSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "event_session_kind_id",
                table: "event_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_event_session_kind_id",
                table: "event_sessions",
                column: "event_session_kind_id");

            migrationBuilder.AddForeignKey(
                name: "fk_event_sessions_event_session_kinds_event_session_kind_id",
                table: "event_sessions",
                column: "event_session_kind_id",
                principalTable: "event_session_kinds",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_event_session_kinds_event_session_kind_id",
                table: "event_sessions");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_event_session_kind_id",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "event_session_kind_id",
                table: "event_sessions");
        }
    }
}
