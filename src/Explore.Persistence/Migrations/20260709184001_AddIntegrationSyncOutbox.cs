// ABOUTME: EF Core migration creating the native integration sync outbox table.
// ABOUTME: Adds durable Listmonk subscriber sync storage with retry and tenant indexes.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationSyncOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_sync_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    source_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subscriber_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subscriber_name = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    subscriber_payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    listmonk_list_id = table.Column<int>(type: "integer", nullable: false),
                    preconfirm_subscriptions = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_sync_outbox", x => x.id);
                    table.ForeignKey(
                        name: "fk_integration_sync_outbox_event_registration_intents_registra",
                        column: x => x.registration_intent_id,
                        principalTable: "event_registration_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_integration_sync_outbox_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_integration_sync_outbox_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_integration_sync_outbox_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_integration_sync_outbox_event_id",
                table: "integration_sync_outbox",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_sync_outbox_registration_intent_id",
                table: "integration_sync_outbox",
                column: "registration_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_sync_outbox_tenant_status",
                table: "integration_sync_outbox",
                columns: new[] { "tenant_id", "status", "last_failure_at" });

            migrationBuilder.CreateIndex(
                name: "ix_integration_sync_outbox_user_id",
                table: "integration_sync_outbox",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_integration_sync_outbox_worker_poll",
                table: "integration_sync_outbox",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_integration_sync_outbox_tenant_source_kind",
                table: "integration_sync_outbox",
                columns: new[] { "tenant_id", "source_type", "source_id", "kind" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_sync_outbox");
        }
    }
}
