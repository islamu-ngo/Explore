// ABOUTME: Finalizes tenant-safe webhook alternate keys, ownership FKs, and Restrict delete behavior.
// ABOUTME: Rejects cross-tenant legacy references instead of silently repairing or deleting ambiguous evidence.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeWebhookTenantConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_effect_receipts_incoming_webhook_messages_",
                table: "incoming_webhook_effect_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_processing_attempts_incoming_webhook_messa",
                table: "incoming_webhook_processing_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_redrive_records_incoming_webhook_messages_",
                table: "incoming_webhook_redrive_records");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_actors_owner_actor_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_tenant_id_",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoint_subscriptions_tenant_id_endpoint_id",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_owner_actor_id",
                table: "webhook_consumers");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_provider_links_tenant_id_id",
                table: "webhook_provider_links",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_endpoint_subscriptions_tenant_id_id",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_incoming_webhook_redrive_records_tenant_id_id",
                table: "incoming_webhook_redrive_records",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_incoming_webhook_processing_attempts_tenant_id_id",
                table: "incoming_webhook_processing_attempts",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "endpoint_id", "event_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_tenant_id_owner_actor_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "owner_actor_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_effect_receipts_incoming_webhook_messages_",
                table: "incoming_webhook_effect_receipts",
                columns: new[] { "tenant_id", "incoming_webhook_message_id" },
                principalTable: "incoming_webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_processing_attempts_incoming_webhook_messa",
                table: "incoming_webhook_processing_attempts",
                columns: new[] { "tenant_id", "incoming_webhook_message_id" },
                principalTable: "incoming_webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_redrive_records_incoming_webhook_messages_",
                table: "incoming_webhook_redrive_records",
                columns: new[] { "tenant_id", "incoming_webhook_message_id" },
                principalTable: "incoming_webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_actors_tenant_id_owner_actor_id",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "owner_actor_id" },
                principalTable: "actors",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_tenant_id_",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "endpoint_id" },
                principalTable: "webhook_endpoints",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_effect_receipts_incoming_webhook_messages_",
                table: "incoming_webhook_effect_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_processing_attempts_incoming_webhook_messa",
                table: "incoming_webhook_processing_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_redrive_records_incoming_webhook_messages_",
                table: "incoming_webhook_redrive_records");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_actors_tenant_id_owner_actor_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_tenant_id_",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_provider_links_tenant_id_id",
                table: "webhook_provider_links");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_endpoint_subscriptions_tenant_id_id",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_tenant_id_owner_actor_id",
                table: "webhook_consumers");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_incoming_webhook_redrive_records_tenant_id_id",
                table: "incoming_webhook_redrive_records");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_incoming_webhook_processing_attempts_tenant_id_id",
                table: "incoming_webhook_processing_attempts");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoint_subscriptions_tenant_id_endpoint_id",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoint_subscriptions_endpoint_event_type",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "endpoint_id", "event_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_owner_actor_id",
                table: "webhook_consumers",
                column: "owner_actor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_effect_receipts_incoming_webhook_messages_",
                table: "incoming_webhook_effect_receipts",
                columns: new[] { "tenant_id", "incoming_webhook_message_id" },
                principalTable: "incoming_webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_processing_attempts_incoming_webhook_messa",
                table: "incoming_webhook_processing_attempts",
                columns: new[] { "tenant_id", "incoming_webhook_message_id" },
                principalTable: "incoming_webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_redrive_records_incoming_webhook_messages_",
                table: "incoming_webhook_redrive_records",
                columns: new[] { "tenant_id", "incoming_webhook_message_id" },
                principalTable: "incoming_webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_actors_owner_actor_id",
                table: "webhook_consumers",
                column: "owner_actor_id",
                principalTable: "actors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_tenant_id_",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "endpoint_id" },
                principalTable: "webhook_endpoints",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
