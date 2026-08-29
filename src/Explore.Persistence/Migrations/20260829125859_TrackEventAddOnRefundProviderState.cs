using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackEventAddOnRefundProviderState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "failed_at",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations",
                sql: "status >= 1 AND status <= 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "failed_at",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations");
        }
    }
}
