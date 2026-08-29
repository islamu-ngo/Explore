using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class TrackEventAddOnRefundProviderState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "ie_event_add_on_refund_allocations",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                table: "ie_event_add_on_refund_allocations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "failed_at",
                table: "ie_event_add_on_refund_allocations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "ie_event_add_on_refund_allocations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                table: "ie_event_add_on_refund_allocations",
                sql: "status >= 1 AND status <= 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                table: "ie_event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "ie_event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "ie_event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "ie_event_add_on_refund_allocations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "ie_event_add_on_refund_allocations");
        }
    }
}
