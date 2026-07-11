// ABOUTME: Adds EventSessionStatus lookup table and nullable schedule fields for draft sessions.
// ABOUTME: Backfills existing sessions to Draft=1 and updates GiST exclusion to skip unscheduled rows.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSessionStatusAndNullableSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_EndAfterStart",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteMatchesTime",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteRange",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteMatchesTime",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteRange",
                table: "event_sessions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "start_time",
                table: "event_sessions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "local_start_time",
                table: "event_sessions",
                type: "time without time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<int>(
                name: "local_start_minute_of_day",
                table: "event_sessions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "local_start_date",
                table: "event_sessions",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "local_end_time",
                table: "event_sessions",
                type: "time without time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<int>(
                name: "local_end_minute_of_day",
                table: "event_sessions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "local_end_date",
                table: "event_sessions",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "end_time",
                table: "event_sessions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            // Add EventSessionStatusId with default=1 (Draft) so existing rows get a valid status
            // before the Restrict FK is applied. Without this, existing rows would have id=0
            // which doesn't exist in event_session_statuses, causing FK violation.
            migrationBuilder.AddColumn<int>(
                name: "event_session_status_id",
                table: "event_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "event_session_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_session_statuses", x => x.id);
                });

            // Seed EventSessionStatus lookup rows (mirror EventSessionStatusEnum values).
            // LookupTableSeeder also handles this at runtime, but migration seeds ensure
            // FK constraints can be validated against existing rows immediately.
            migrationBuilder.InsertData(
                table: "event_session_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "DRAFT", "Draft", "Session is in draft state and not visible to the public" },
                    { 2, "SUBMITTED", "Submitted", "Session has been submitted for review" },
                    { 3, "UNDER_REVIEW", "UnderReview", "Session is currently being reviewed" },
                    { 4, "APPROVED", "Approved", "Session has been approved but is not yet published" },
                    { 5, "PUBLISHED", "Published", "Session is published and visible to the public" },
                    { 6, "REJECTED", "Rejected", "Session was rejected during review" },
                    { 7, "CANCELLED", "Cancelled", "Session has been cancelled" },
                    { 8, "ARCHIVED", "Archived", "Session has been archived" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_event_session_status_id",
                table: "event_sessions",
                column: "event_session_status_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_EndAfterStart",
                table: "event_sessions",
                sql: "end_time IS NULL OR start_time IS NULL OR end_time > start_time");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteMatchesTime",
                table: "event_sessions",
                sql: "local_end_minute_of_day IS NULL OR local_end_time IS NULL OR local_end_minute_of_day = ((EXTRACT(HOUR FROM local_end_time)::int * 60) + EXTRACT(MINUTE FROM local_end_time)::int)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteRange",
                table: "event_sessions",
                sql: "local_end_minute_of_day IS NULL OR local_end_minute_of_day BETWEEN 0 AND 1439");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteMatchesTime",
                table: "event_sessions",
                sql: "local_start_minute_of_day IS NULL OR local_start_time IS NULL OR local_start_minute_of_day = ((EXTRACT(HOUR FROM local_start_time)::int * 60) + EXTRACT(MINUTE FROM local_start_time)::int)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteRange",
                table: "event_sessions",
                sql: "local_start_minute_of_day IS NULL OR local_start_minute_of_day BETWEEN 0 AND 1439");

            migrationBuilder.AddForeignKey(
                name: "fk_event_sessions_event_session_statuses_event_session_status_",
                table: "event_sessions",
                column: "event_session_status_id",
                principalTable: "event_session_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Update GiST exclusion constraint to skip unscheduled (nullable start/end) sessions.
            // EF scaffolder does not detect Npgsql HasPostgresExclusionConstraint predicate changes,
            // so this must be handled via raw SQL. The partial WHERE clause ensures tstzrange
            // is never constructed from NULL values (which would create unbounded ranges).
            migrationBuilder.Sql(@"
                ALTER TABLE event_sessions DROP CONSTRAINT IF EXISTS ""EX_EventSession_RoomNoOverlap"";
                ALTER TABLE event_sessions ADD CONSTRAINT ""EX_EventSession_RoomNoOverlap""
                EXCLUDE USING gist (
                    room_id WITH =,
                    tstzrange(start_time, end_time, '[)') WITH &&
                )
                WHERE (is_deleted = false AND room_id IS NOT NULL AND start_time IS NOT NULL AND end_time IS NOT NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert GiST exclusion to the original predicate (without start/end null check).
            migrationBuilder.Sql(@"
                ALTER TABLE event_sessions DROP CONSTRAINT IF EXISTS ""EX_EventSession_RoomNoOverlap"";
                ALTER TABLE event_sessions ADD CONSTRAINT ""EX_EventSession_RoomNoOverlap""
                EXCLUDE USING gist (
                    room_id WITH =,
                    tstzrange(start_time, end_time, '[)') WITH &&
                )
                WHERE (is_deleted = false AND room_id IS NOT NULL);
            ");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_event_session_statuses_event_session_status_",
                table: "event_sessions");

            // Delete seeded EventSessionStatus rows.
            migrationBuilder.DeleteData(
                table: "event_session_statuses",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            migrationBuilder.DropTable(
                name: "event_session_statuses");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_event_session_status_id",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_EndAfterStart",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteMatchesTime",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteRange",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteMatchesTime",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteRange",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "event_session_status_id",
                table: "event_sessions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "start_time",
                table: "event_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "local_start_time",
                table: "event_sessions",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "local_start_minute_of_day",
                table: "event_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "local_start_date",
                table: "event_sessions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "local_end_time",
                table: "event_sessions",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "local_end_minute_of_day",
                table: "event_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "local_end_date",
                table: "event_sessions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "end_time",
                table: "event_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_EndAfterStart",
                table: "event_sessions",
                sql: "end_time > start_time");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteMatchesTime",
                table: "event_sessions",
                sql: "local_end_minute_of_day = ((EXTRACT(HOUR FROM local_end_time)::int * 60) + EXTRACT(MINUTE FROM local_end_time)::int)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalEndMinuteRange",
                table: "event_sessions",
                sql: "local_end_minute_of_day BETWEEN 0 AND 1439");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteMatchesTime",
                table: "event_sessions",
                sql: "local_start_minute_of_day = ((EXTRACT(HOUR FROM local_start_time)::int * 60) + EXTRACT(MINUTE FROM local_start_time)::int)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalStartMinuteRange",
                table: "event_sessions",
                sql: "local_start_minute_of_day BETWEEN 0 AND 1439");
        }
    }
}
