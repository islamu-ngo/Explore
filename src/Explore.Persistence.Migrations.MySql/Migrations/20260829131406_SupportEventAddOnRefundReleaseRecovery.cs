using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class SupportEventAddOnRefundReleaseRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                table: "ie_event_add_on_refund_allocations");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                table: "ie_event_add_on_refund_allocations",
                sql: "status >= 1 AND status <= 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                table: "ie_event_add_on_refund_allocations");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_add_on_refund_allocations_status",
                table: "ie_event_add_on_refund_allocations",
                sql: "status >= 1 AND status <= 3");
        }
    }
}
