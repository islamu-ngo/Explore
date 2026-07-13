// ABOUTME: Adds normalized webhook provider-binding authority and pending webhook schema foundations.
// ABOUTME: Creates stable verification lookup rows, tenant-safe identities, and fenced binding writes.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookProviderBindingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_webhook_messages_tenant_payload_retention",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_webhook_messages_tenant_status_created",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ux_webhook_messages_tenant_provider_message",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ux_incoming_webhook_messages_tenant_provider_idempotency",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "provider_mode",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "response_body_preview",
                table: "webhook_delivery_attempts");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "webhook_messages",
                newName: "payload_provenance_id");

            migrationBuilder.AlterColumn<string>(
                name: "payload_hash",
                table: "webhook_messages",
                type: "character varying(71)",
                maxLength: 71,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<byte[]>(
                name: "payload_bytes",
                table: "webhook_messages",
                type: "bytea",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE webhook_messages
                SET payload_bytes = CASE
                        WHEN payload_json IS NULL THEN NULL
                        ELSE convert_to(payload_json::text, 'UTF8')
                    END,
                    payload_provenance_id = 2;
                """);

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "webhook_messages");

            migrationBuilder.AddColumn<string>(
                name: "auto_pause_reason",
                table: "webhook_endpoints",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "auto_paused_at",
                table: "webhook_endpoints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "circuit_opened_at",
                table: "webhook_endpoints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "consecutive_failure_count",
                table: "webhook_endpoints",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "delivery_state_version",
                table: "webhook_endpoints",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_resumed_at",
                table: "webhook_endpoints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_resumed_by",
                table: "webhook_endpoints",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "processing_fence",
                table: "webhook_delivery_attempts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_lease_expires_at",
                table: "webhook_delivery_attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE incoming_webhook_messages
                SET verified_at = COALESCE(verified_at, received_at);
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "verified_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "safe_detail",
                table: "incoming_webhook_messages",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider_message_id",
                table: "incoming_webhook_messages",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "payload_hash",
                table: "incoming_webhook_messages",
                type: "character varying(71)",
                maxLength: 71,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "incoming_webhook_messages",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "incoming_webhook_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "dead_lettered_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ignored_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "payload_bytes",
                table: "incoming_webhook_messages",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "payload_conflict_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "processing_fence",
                table: "incoming_webhook_messages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "processing_generation",
                table: "incoming_webhook_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_lease_expires_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_lease_owner",
                table: "incoming_webhook_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "processing_lease_token",
                table: "incoming_webhook_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_started_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "settled_by_effect_receipt_id",
                table: "incoming_webhook_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "settled_effect_kind",
                table: "incoming_webhook_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "settlement_source",
                table: "incoming_webhook_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE incoming_webhook_messages
                SET payload_bytes = convert_to(COALESCE(payload_json::text, 'null'), 'UTF8'),
                    processing_generation = 1;
                """);

            migrationBuilder.DropColumn(
                name: "payload_json",
                table: "incoming_webhook_messages");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_incoming_webhook_messages_tenant_id",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "incoming_webhook_effect_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incoming_webhook_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect_kind = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(71)", maxLength: 71, nullable: false),
                    processing_generation = table.Column<int>(type: "integer", nullable: false),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    safe_result_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_effect_receipts", x => x.id);
                    table.UniqueConstraint("ak_incoming_webhook_effect_receipts_tenant_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_incoming_webhook_effect_receipts_payload_hash", "payload_hash ~ '^sha256:[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_incoming_webhook_effect_receipts_processing_generation", "processing_generation >= 1");
                    table.ForeignKey(
                        name: "fk_incoming_webhook_effect_receipts_incoming_webhook_messages_",
                        columns: x => new { x.tenant_id, x.incoming_webhook_message_id },
                        principalTable: "incoming_webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_incoming_webhook_effect_receipts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incoming_webhook_processing_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incoming_webhook_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processing_generation = table.Column<int>(type: "integer", nullable: false),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    safe_detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_processing_attempts", x => x.id);
                    table.CheckConstraint("ck_incoming_webhook_processing_attempts_fence", "processing_fence >= 0");
                    table.CheckConstraint("ck_incoming_webhook_processing_attempts_generation", "processing_generation >= 1");
                    table.CheckConstraint("ck_incoming_webhook_processing_attempts_number", "attempt_number >= 0");
                    table.ForeignKey(
                        name: "fk_incoming_webhook_processing_attempts_incoming_webhook_messa",
                        columns: x => new { x.tenant_id, x.incoming_webhook_message_id },
                        principalTable: "incoming_webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_incoming_webhook_processing_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incoming_webhook_redrive_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incoming_webhook_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_processing_generation = table.Column<int>(type: "integer", nullable: false),
                    target_processing_generation = table.Column<int>(type: "integer", nullable: false),
                    result = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_redrive_records", x => x.id);
                    table.CheckConstraint("ck_incoming_webhook_redrive_records_generation_order", "target_processing_generation > source_processing_generation AND source_processing_generation >= 1");
                    table.ForeignKey(
                        name: "fk_incoming_webhook_redrive_records_incoming_webhook_messages_",
                        columns: x => new { x.tenant_id, x.incoming_webhook_message_id },
                        principalTable: "incoming_webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_incoming_webhook_redrive_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_provider_binding_verification_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_binding_verification_states", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "webhook_provider_binding_verification_states",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "LEGACY_UNVERIFIED", "Legacy unverified", "Legacy binding identity could not be proven and cannot grant provider authority" },
                    { 2, "PENDING", "Pending", "Binding is awaiting provider ownership verification" },
                    { 3, "VERIFIED", "Verified", "Provider ownership matches the persisted tenant and webhook consumer" },
                    { 4, "REJECTED", "Rejected", "Provider ownership verification was rejected" },
                    { 5, "REVOKED", "Revoked", "Previously verified provider authority has been revoked" }
                });

            migrationBuilder.CreateTable(
                name: "webhook_consumer_provider_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_consumer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_kind_id = table.Column<int>(type: "integer", nullable: false),
                    provider_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_environment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_environment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    application_uid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_application_uid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    external_application_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    normalized_external_application_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verification_state_id = table.Column<int>(type: "integer", nullable: false),
                    verified_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_webhook_consumer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    capabilities = table.Column<long>(type: "bigint", nullable: false),
                    governance_allowed_capabilities = table.Column<long>(type: "bigint", nullable: false),
                    capability_resolution_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capabilities_resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_version = table.Column<long>(type: "bigint", nullable: false),
                    verification_fence = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_consumer_provider_bindings", x => x.id);
                    table.UniqueConstraint("ak_webhook_consumer_provider_bindings_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_webhook_consumer_provider_bindings_concurrency_version_posi~", "concurrency_version > 0");
                    table.CheckConstraint("ck_webhook_consumer_provider_bindings_verification_fence_posit~", "verification_fence > 0");
                    table.ForeignKey(
                        name: "fk_webhook_consumer_provider_bindings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_consumer_provider_bindings_webhook_consumers_tenant",
                        columns: x => new { x.tenant_id, x.webhook_consumer_id },
                        principalTable: "webhook_consumers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_consumer_provider_bindings_webhook_provider_binding",
                        column: x => x.verification_state_id,
                        principalTable: "webhook_provider_binding_verification_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_tenant_created",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_tenant_payload_retention",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "payload_retention_until" },
                filter: "payload_bytes IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_messages_payload_hash",
                table: "webhook_messages",
                sql: "payload_hash ~ '^sha256:[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_messages_payload_provenance",
                table: "webhook_messages",
                sql: "payload_provenance_id > 0");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_status_tenant_id",
                table: "webhook_endpoints",
                columns: new[] { "status", "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_active_lease_caps",
                table: "webhook_delivery_attempts",
                columns: new[] { "status", "processing_lease_expires_at", "tenant_id", "endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_claim_due",
                table: "incoming_webhook_messages",
                columns: new[] { "status", "next_attempt_at", "processing_lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_tenant_provider_idempotency",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "provider", "idempotency_key" },
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_messages_payload_hash",
                table: "incoming_webhook_messages",
                sql: "payload_hash ~ '^sha256:[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_messages_processing_fence",
                table: "incoming_webhook_messages",
                sql: "processing_fence >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_messages_processing_generation",
                table: "incoming_webhook_messages",
                sql: "processing_generation >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_effect_receipts_tenant_applied",
                table: "incoming_webhook_effect_receipts",
                columns: new[] { "tenant_id", "applied_at" });

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_effect_receipts_identity",
                table: "incoming_webhook_effect_receipts",
                columns: new[] { "tenant_id", "incoming_webhook_message_id", "effect_kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_processing_attempts_tenant_recorded",
                table: "incoming_webhook_processing_attempts",
                columns: new[] { "tenant_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_processing_attempts_evidence",
                table: "incoming_webhook_processing_attempts",
                columns: new[] { "tenant_id", "incoming_webhook_message_id", "processing_generation", "processing_fence", "outcome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_redrive_records_target_generation",
                table: "incoming_webhook_redrive_records",
                columns: new[] { "tenant_id", "incoming_webhook_message_id", "target_processing_generation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumer_provider_bindings_verification_state_id",
                table: "webhook_consumer_provider_bindings",
                column: "verification_state_id");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_tenant_consumer_provider_environment",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "webhook_consumer_id", "provider_kind_id", "normalized_environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_tenant_provider_environment_application_uid",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "provider_kind_id", "normalized_environment", "normalized_application_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_bindings_tenant_provider_environment_external_app",
                table: "webhook_consumer_provider_bindings",
                columns: new[] { "tenant_id", "provider_kind_id", "normalized_environment", "normalized_external_application_id" },
                unique: true,
                filter: "normalized_external_application_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_binding_verification_states_master_code",
                table: "webhook_provider_binding_verification_states",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incoming_webhook_effect_receipts");

            migrationBuilder.DropTable(
                name: "incoming_webhook_processing_attempts");

            migrationBuilder.DropTable(
                name: "incoming_webhook_redrive_records");

            migrationBuilder.DropTable(
                name: "webhook_consumer_provider_bindings");

            migrationBuilder.DeleteData(
                table: "webhook_provider_binding_verification_states",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5 });

            migrationBuilder.DropTable(
                name: "webhook_provider_binding_verification_states");

            migrationBuilder.DropIndex(
                name: "ix_webhook_messages_tenant_created",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_webhook_messages_tenant_payload_retention",
                table: "webhook_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_messages_payload_hash",
                table: "webhook_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_messages_payload_provenance",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoints_status_tenant_id",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_active_lease_caps",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_incoming_webhook_messages_tenant_id",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_claim_due",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_tenant_provider_idempotency",
                table: "incoming_webhook_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_messages_payload_hash",
                table: "incoming_webhook_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_messages_processing_fence",
                table: "incoming_webhook_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_messages_processing_generation",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_bytes",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "auto_pause_reason",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "auto_paused_at",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "circuit_opened_at",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "consecutive_failure_count",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "delivery_state_version",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "last_resumed_at",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "last_resumed_by",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "processing_fence",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "processing_lease_expires_at",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "dead_lettered_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "ignored_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_bytes",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_conflict_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "processing_fence",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "processing_generation",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "processing_lease_expires_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "processing_lease_owner",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "processing_lease_token",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "processing_started_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "settled_by_effect_receipt_id",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "settled_effect_kind",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "settlement_source",
                table: "incoming_webhook_messages");

            migrationBuilder.RenameColumn(
                name: "payload_provenance_id",
                table: "webhook_messages",
                newName: "status");

            migrationBuilder.AlterColumn<string>(
                name: "payload_hash",
                table: "webhook_messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(71)",
                oldMaxLength: 71);

            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "webhook_messages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                table: "webhook_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "provider_mode",
                table: "webhook_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at",
                table: "webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "response_body_preview",
                table: "webhook_delivery_attempts",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "verified_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "safe_detail",
                table: "incoming_webhook_messages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider_message_id",
                table: "incoming_webhook_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "payload_hash",
                table: "incoming_webhook_messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(71)",
                oldMaxLength: 71);

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                table: "incoming_webhook_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payload_json",
                table: "incoming_webhook_messages",
                type: "jsonb",
                nullable: true);

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
                name: "ux_webhook_messages_tenant_provider_message",
                table: "webhook_messages",
                columns: new[] { "tenant_id", "provider_message_id" },
                unique: true,
                filter: "provider_message_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_messages_tenant_provider_idempotency",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "provider", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }
    }
}
