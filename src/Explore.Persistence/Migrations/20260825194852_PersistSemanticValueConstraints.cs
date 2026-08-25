using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistSemanticValueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "lookup_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "bytea",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(44)",
                oldFixedLength: true,
                oldMaxLength: 44);

            migrationBuilder.AlterColumn<byte[]>(
                name: "locator_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "bytea",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(44)",
                oldFixedLength: true,
                oldMaxLength: 44);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LocationPii_CoordinateShape",
                schema: "islamu_event",
                table: "location_pii",
                sql: "(latitude IS NULL AND longitude IS NULL)\nOR (latitude IS NOT NULL AND longitude IS NOT NULL\n    AND latitude BETWEEN -90 AND 90\n    AND longitude BETWEEN -180 AND 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventTicketType_MoneyNonnegative",
                schema: "islamu_event",
                table: "event_ticket_types",
                sql: "(fixed_price_minor IS NULL OR fixed_price_minor >= 0)\nAND (minimum_price_minor IS NULL OR minimum_price_minor >= 0)\nAND (suggested_price_minor IS NULL OR suggested_price_minor >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_LocalDateRange",
                schema: "islamu_event",
                table: "event_sessions",
                sql: "local_end_date IS NULL OR local_start_date IS NULL OR local_end_date >= local_start_date");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAgendaItem_LocalDateRange",
                schema: "islamu_event",
                table: "event_agenda_items",
                sql: "local_end_date >= local_start_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LocationPii_CoordinateShape",
                schema: "islamu_event",
                table: "location_pii");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventTicketType_MoneyNonnegative",
                schema: "islamu_event",
                table: "event_ticket_types");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_LocalDateRange",
                schema: "islamu_event",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAgendaItem_LocalDateRange",
                schema: "islamu_event",
                table: "event_agenda_items");

            migrationBuilder.AlterColumn<string>(
                name: "lookup_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "character(44)",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldFixedLength: true,
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "locator_digest",
                schema: "islamu_event",
                table: "admission_recovery_capabilities",
                type: "character(44)",
                fixedLength: true,
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldFixedLength: true,
                oldMaxLength: 32);
        }
    }
}
