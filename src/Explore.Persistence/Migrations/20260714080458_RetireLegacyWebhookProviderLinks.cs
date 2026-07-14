using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetireLegacyWebhookProviderLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_provider_links");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_provider_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    external_app_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_endpoint_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_error_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    sync_state = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_links", x => x.id);
                    table.UniqueConstraint("ak_webhook_provider_links_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_webhook_provider_links_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_links_webhook_consumers_tenant_id_consumer",
                        columns: x => new { x.tenant_id, x.consumer_id },
                        principalTable: "webhook_consumers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_links_webhook_endpoints_tenant_id_endpoint",
                        columns: x => new { x.tenant_id, x.endpoint_id },
                        principalTable: "webhook_endpoints",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_links_webhook_messages_tenant_id_message_id",
                        columns: x => new { x.tenant_id, x.message_id },
                        principalTable: "webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_links_provider_sync_state",
                table: "webhook_provider_links",
                columns: new[] { "provider", "sync_state", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_links_tenant_id_consumer_id",
                table: "webhook_provider_links",
                columns: new[] { "tenant_id", "consumer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_links_tenant_id_endpoint_id",
                table: "webhook_provider_links",
                columns: new[] { "tenant_id", "endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_links_tenant_id_message_id",
                table: "webhook_provider_links",
                columns: new[] { "tenant_id", "message_id" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_links_tenant_provider_app",
                table: "webhook_provider_links",
                columns: new[] { "tenant_id", "provider", "external_app_id" },
                unique: true,
                filter: "external_app_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_links_tenant_provider_endpoint",
                table: "webhook_provider_links",
                columns: new[] { "tenant_id", "provider", "external_endpoint_id" },
                unique: true,
                filter: "external_endpoint_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_links_tenant_provider_message",
                table: "webhook_provider_links",
                columns: new[] { "tenant_id", "provider", "external_message_id" },
                unique: true,
                filter: "external_message_id IS NOT NULL");
        }
    }
}
