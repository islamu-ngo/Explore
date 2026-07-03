using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookSubsystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incoming_webhook_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    headers_json = table.Column<string>(type: "jsonb", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    safe_detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_incoming_webhook_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_consumers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consumer_kind = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    provider_mode = table.Column<int>(type: "integer", nullable: false),
                    external_provider_app_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_consumers", x => x.id);
                    table.UniqueConstraint("ak_webhook_consumers_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_webhook_consumers_actors_owner_actor_id",
                        column: x => x.owner_actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_consumers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_consumers_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_event_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    schema_json = table.Column<string>(type: "jsonb", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    payload_retention_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_endpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    secret_ref = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    secret_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    previous_secret_ref = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    previous_secret_valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_endpoint_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 8),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: true),
                    last_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_endpoints", x => x.id);
                    table.UniqueConstraint("ak_webhook_endpoints_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_webhook_endpoints_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_endpoints_webhook_consumers_tenant_id_consumer_id",
                        columns: x => new { x.tenant_id, x.consumer_id },
                        principalTable: "webhook_consumers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_retention_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payload_cleared_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_mode = table.Column<int>(type: "integer", nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_messages", x => x.id);
                    table.UniqueConstraint("ak_webhook_messages_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_webhook_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_messages_webhook_consumers_tenant_id_consumer_id",
                        columns: x => new { x.tenant_id, x.consumer_id },
                        principalTable: "webhook_consumers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_endpoint_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_endpoint_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_webhook_endpoint_subscriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_endpoint_subscriptions_webhook_endpoints_tenant_id_",
                        columns: x => new { x.tenant_id, x.endpoint_id },
                        principalTable: "webhook_endpoints",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_webhook_endpoint_subscriptions_webhook_event_types_event_ty",
                        column: x => x.event_type_id,
                        principalTable: "webhook_event_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_delivery_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    response_body_preview = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_delivery_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_webhook_delivery_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_delivery_attempts_webhook_endpoints_tenant_id_endpo",
                        columns: x => new { x.tenant_id, x.endpoint_id },
                        principalTable: "webhook_endpoints",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_delivery_attempts_webhook_messages_tenant_id_messag",
                        columns: x => new { x.tenant_id, x.message_id },
                        principalTable: "webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhook_provider_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    endpoint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    external_app_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_endpoint_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sync_state = table.Column<int>(type: "integer", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_links", x => x.id);
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
                name: "ix_incoming_webhook_messages_tenant_status_received",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "status", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_messages_tenant_provider_idempotency",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "provider", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_messages_tenant_provider_message",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "provider", "provider_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_owner_actor_id",
                table: "webhook_consumers",
                column: "owner_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_owner_user_id",
                table: "webhook_consumers",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_tenant_status_provider",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "status", "provider_mode" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_tenant_external_app",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "external_provider_app_id" },
                unique: true,
                filter: "external_provider_app_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumers_tenant_name",
                table: "webhook_consumers",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_tenant_endpoint_status",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "endpoint_id", "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_tenant_id_message_id",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "message_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_worker_poll",
                table: "webhook_delivery_attempts",
                columns: new[] { "status", "scheduled_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_delivery_attempts_message_endpoint_attempt",
                table: "webhook_delivery_attempts",
                columns: new[] { "message_id", "endpoint_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoint_subscriptions_event_type_id",
                table: "webhook_endpoint_subscriptions",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoint_subscriptions_tenant_event_type",
                table: "webhook_endpoint_subscriptions",
                columns: new[] { "tenant_id", "event_type_id", "is_enabled" });

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
                name: "ix_webhook_endpoints_tenant_consumer_status",
                table: "webhook_endpoints",
                columns: new[] { "tenant_id", "consumer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoints_tenant_provider_endpoint",
                table: "webhook_endpoints",
                columns: new[] { "tenant_id", "provider_endpoint_id" },
                unique: true,
                filter: "provider_endpoint_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_event_types_group_enabled_public",
                table: "webhook_event_types",
                columns: new[] { "group_name", "is_enabled", "is_public" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_event_types_name",
                table: "webhook_event_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_tenant_aggregate",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "aggregate_kind", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_tenant_id_consumer_id",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "consumer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_tenant_payload_retention",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "payload_retention_until" },
                filter: "payload_json IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_tenant_status_created",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_messages_tenant_event",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "event_type", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_messages_tenant_provider_message",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "provider_message_id" },
                unique: true,
                filter: "provider_message_id IS NOT NULL");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incoming_webhook_messages");

            migrationBuilder.DropTable(
                name: "webhook_delivery_attempts");

            migrationBuilder.DropTable(
                name: "webhook_endpoint_subscriptions");

            migrationBuilder.DropTable(
                name: "webhook_provider_links");

            migrationBuilder.DropTable(
                name: "webhook_event_types");

            migrationBuilder.DropTable(
                name: "webhook_endpoints");

            migrationBuilder.DropTable(
                name: "webhook_messages");

            migrationBuilder.DropTable(
                name: "webhook_consumers");
        }
    }
}
