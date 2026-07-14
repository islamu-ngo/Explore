// ABOUTME: Persists verified provider-binding provenance for incoming webhook authority resolution.
// ABOUTME: Enforces globally unique normalized provider application identities and tenant-safe inbox linkage.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomingWebhookAuthorityProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_webhook_consumer_provider_bindings_provider_kind_id",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.AddColumn<Guid>(
                name: "webhook_consumer_provider_binding_id",
                table: "incoming_webhook_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_provider_application_identity",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "provider_kind_id", "normalized_environment", "normalized_external_application_id", "normalized_application_uid" },
                unique: true,
                filter: "normalized_external_application_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_binding_received",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "webhook_consumer_provider_binding_id", "received_at" },
                filter: "webhook_consumer_provider_binding_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "webhook_consumer_provider_binding_id" },
                principalTable: "webhook_consumer_provider_bindings",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_messages_webhook_consumer_provider_binding",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ux_webhook_provider_bindings_provider_application_identity",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_binding_received",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "webhook_consumer_provider_binding_id",
                table: "incoming_webhook_messages");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumer_provider_bindings_provider_kind_id",
                table: "webhook_consumer_provider_bindings",
                column: "provider_kind_id");
        }
    }
}
