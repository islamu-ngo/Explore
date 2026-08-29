using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BindEventAddOnRefundAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_event_add_on_refund_allocations_refund_attempts_548356fb8132",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "refund_operation_id" },
                principalSchema: "islamu_event",
                principalTable: "refund_attempts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_add_on_refund_allocations_refund_attempts_548356fb8132",
                schema: "islamu_event",
                table: "event_add_on_refund_allocations");
        }
    }
}
