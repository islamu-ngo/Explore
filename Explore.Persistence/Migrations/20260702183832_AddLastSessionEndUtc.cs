using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLastSessionEndUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_session_end_utc",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE events e
                SET last_session_end_utc = (
                    SELECT COALESCE(es.end_time, es.start_time + INTERVAL '1 day')
                    FROM event_sessions es
                    WHERE es.event_id = e.id 
                      AND es.is_deleted = false 
                      AND es.event_session_status_id = 1 
                      AND es.start_time IS NOT NULL
                    ORDER BY es.start_time DESC, es.sort_order DESC, es.id DESC
                    LIMIT 1
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_session_end_utc",
                table: "events");
        }
    }
}
