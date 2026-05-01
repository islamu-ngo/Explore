using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceRegistrationCapacityAndWaitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations");

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations",
                columns: new[] { "event_session_id", "user_id" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations");

            migrationBuilder.CreateIndex(
                name: "ix_eventregistrations_session_user",
                table: "event_registrations",
                columns: new[] { "event_session_id", "user_id" },
                unique: true);
        }
    }
}
