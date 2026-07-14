// ABOUTME: Adds tenant-safe relational provenance from settled incoming messages to effect receipts.
// ABOUTME: Prevents settlement from referencing a receipt owned by another tenant or inbox identity.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomingWebhookSettlementReceiptConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_id_settled_by_effect_recei",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "settled_by_effect_receipt_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_messages_incoming_webhook_effect_receipts_",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "settled_by_effect_receipt_id" },
                principalTable: "incoming_webhook_effect_receipts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_messages_incoming_webhook_effect_receipts_",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_id_settled_by_effect_recei",
                table: "incoming_webhook_messages");
        }
    }
}
