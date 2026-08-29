using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class BindEventAddOnRefundAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_ie_event_add_on_refund_allocations_ie_refund_attempt_0be83854",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "refund_operation_id" },
                principalTable: "ie_refund_attempts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ie_event_add_on_refund_allocations_ie_refund_attempt_0be83854",
                table: "ie_event_add_on_refund_allocations");
        }
    }
}
