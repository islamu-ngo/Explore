using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomingWebhookEffectOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incoming_webhook_effect_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incoming_webhook_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_decision_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    effect_kind = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_sha256 = table.Column<string>(type: "character varying(71)", maxLength: 71, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_effect_outbox", x => x.id);
                    table.UniqueConstraint("ak_incoming_webhook_effect_outbox_tenant_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_incoming_webhook_effect_outbox_payload_sha256", "payload_sha256 ~ '^sha256:[0-9a-f]{64}$'");
                    table.ForeignKey(
                        name: "fk_incoming_webhook_effect_outbox_incoming_webhook_messages_te",
                        columns: x => new { x.tenant_id, x.incoming_webhook_message_id },
                        principalTable: "incoming_webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incoming_webhook_effect_outbox_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_effect_outbox_worker_poll",
                table: "incoming_webhook_effect_outbox",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_effect_outbox_message_effect",
                table: "incoming_webhook_effect_outbox",
                columns: new[] { "tenant_id", "incoming_webhook_message_id", "effect_kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_effect_outbox_provider_decision",
                table: "incoming_webhook_effect_outbox",
                columns: new[] { "tenant_id", "provider", "provider_decision_id", "effect_kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incoming_webhook_effect_outbox");

        }
    }
}
