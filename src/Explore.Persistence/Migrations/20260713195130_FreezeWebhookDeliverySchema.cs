// ABOUTME: Freezes normalized webhook lookups, exact-payload evidence, and durable delivery authority tables.
// ABOUTME: Classifies legacy JSON bytes explicitly before adding relational constraints and tenant-safe indexes.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FreezeWebhookDeliverySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_messages_tenant_id_messag",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoints_status_tenant_id",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_active_lease_caps",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_tenant_id_message_id",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_worker_poll",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ux_webhook_delivery_attempts_message_endpoint_attempt",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_claim_due",
                table: "incoming_webhook_messages");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "webhook_endpoints",
                newName: "status_id");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "webhook_delivery_attempts",
                newName: "outcome_id");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "webhook_consumers",
                newName: "status_id");

            migrationBuilder.RenameColumn(
                name: "provider_mode",
                table: "webhook_consumers",
                newName: "provider_mode_id");

            migrationBuilder.RenameColumn(
                name: "consumer_kind",
                table: "webhook_consumers",
                newName: "consumer_kind_id");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "incoming_webhook_messages",
                newName: "status_id");

            migrationBuilder.AddColumn<string>(
                name: "content_encoding",
                table: "webhook_messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "webhook_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "materialized_at",
                table: "webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "occurred_at",
                table: "webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "payload_byte_length",
                table: "webhook_messages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<byte[]>(
                name: "payload_bytes",
                table: "incoming_webhook_messages",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "content_encoding",
                table: "incoming_webhook_messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "incoming_webhook_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "payload_byte_length",
                table: "incoming_webhook_messages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "payload_cleared_at",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payload_provenance_id",
                table: "incoming_webhook_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "payload_retention_until",
                table: "incoming_webhook_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddUniqueConstraint(
                name: "ak_webhook_delivery_attempts_tenant_id_id",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "incoming_webhook_message_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_message_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_consumer_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_consumer_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_consumer_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_consumer_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_delivery_attempt_outcomes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_delivery_attempt_outcomes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_endpoint_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_endpoint_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_local_delivery_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_local_delivery_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_payload_provenances",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_payload_provenances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_provider_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_provider_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_provider_publication_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_publication_statuses", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "incoming_webhook_message_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "VERIFIED", "Verified", "Input verification succeeded and is ready to claim" },
                    { 2, "PROCESSING", "Processing", "Inbox row has an active fenced claim" },
                    { 3, "RETRY_DUE", "Retry due", "Inbox row is waiting for a bounded retry" },
                    { 4, "PROCESSED", "Processed", "Effect receipt and settlement completed" },
                    { 5, "IGNORED", "Ignored", "Verified input required no business effect" },
                    { 6, "REJECTED_PERMANENT", "Rejected permanently", "Input cannot be processed safely" },
                    { 7, "DEAD_LETTERED", "Dead-lettered", "Input exhausted automatic processing" },
                    { 8, "PAYLOAD_CONFLICT", "Payload conflict", "Provider identity was reused with different exact bytes" }
                });

            migrationBuilder.InsertData(
                table: "webhook_consumer_kinds",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "TENANT", "Tenant", "Tenant-owned webhook consumer" },
                    { 2, "ORGANIZATION", "Organization", "Organization-owned webhook consumer" },
                    { 3, "GROUP", "Group", "Group-owned webhook consumer" },
                    { 4, "USER", "User", "User-owned webhook consumer" },
                    { 5, "SYSTEM_INTEGRATION", "System integration", "System integration webhook consumer" }
                });

            migrationBuilder.InsertData(
                table: "webhook_consumer_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "ACTIVE", "Active", "Consumer may receive newly materialized webhook work" },
                    { 2, "DISABLED", "Disabled", "Consumer is disabled for new webhook work" },
                    { 3, "ARCHIVED", "Archived", "Consumer is retained as historical evidence" }
                });

            migrationBuilder.InsertData(
                table: "webhook_delivery_attempt_outcomes",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "SCHEDULED", "Scheduled", "Attempt was scheduled for delivery" },
                    { 2, "SENDING", "Sending", "Attempt entered provider handoff" },
                    { 3, "SUCCEEDED", "Succeeded", "Attempt received a successful response" },
                    { 4, "FAILED", "Failed", "Attempt failed with safe classified evidence" },
                    { 5, "ABANDONED", "Abandoned", "Attempt was not eligible for further delivery" }
                });

            migrationBuilder.InsertData(
                table: "webhook_endpoint_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "ACTIVE", "Active", "Endpoint accepts newly materialized Local work" },
                    { 2, "DISABLED", "Disabled", "Endpoint is administratively disabled" },
                    { 3, "AUTO_PAUSED", "Auto-paused", "Endpoint was paused by bounded failure policy" },
                    { 4, "ARCHIVED", "Archived", "Endpoint is retained as historical evidence" }
                });

            migrationBuilder.InsertData(
                table: "webhook_local_delivery_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "PENDING", "Pending", "Local target is waiting for its first claim" },
                    { 2, "DELIVERING", "Delivering", "Local target has an active fenced delivery claim" },
                    { 3, "RETRY_DUE", "Retry due", "Local target is waiting for a bounded retry" },
                    { 4, "SUCCEEDED", "Succeeded", "Local target completed successfully" },
                    { 5, "DEAD_LETTERED", "Dead-lettered", "Local target exhausted automatic delivery" },
                    { 6, "ABANDONED", "Abandoned", "Local target was explicitly abandoned" }
                });

            migrationBuilder.InsertData(
                table: "webhook_payload_provenances",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "EXACT_BYTES", "Exact bytes", "Persisted bytes are the authoritative received or serialized sequence" },
                    { 2, "LEGACY_JSON_CANONICALIZED", "Legacy JSON canonicalized", "Legacy jsonb was canonicalized because original byte formatting cannot be recovered" }
                });

            migrationBuilder.InsertData(
                table: "webhook_provider_kinds",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "LOCAL", "Local", "Platform-owned direct HTTP delivery" },
                    { 2, "SVIX", "Svix", "Svix application delivery provider" }
                });

            migrationBuilder.InsertData(
                table: "webhook_provider_modes",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "DISABLED", "Disabled", "Webhook delivery is disabled" },
                    { 2, "LOCAL", "Local", "Deliver through the platform Local provider" },
                    { 3, "SVIX", "Svix", "Publish through a verified Svix binding" },
                    { 4, "COMPOSITE", "Composite", "Materialize independent Local and provider work" },
                    { 5, "DRY_RUN", "Dry run", "Materialize evidence without network delivery" }
                });

            migrationBuilder.InsertData(
                table: "webhook_provider_publication_statuses",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "PREPARED", "Prepared", "Provider publication is durably prepared" },
                    { 2, "PUBLISHING", "Publishing", "Provider publication has an active fenced claim" },
                    { 3, "PROVIDER_QUEUED", "Provider queued", "Provider accepted the publication" },
                    { 4, "RETRY_DUE", "Retry due", "Provider publication is waiting for a bounded retry" },
                    { 5, "PUBLICATION_UNKNOWN", "Publication unknown", "Provider acceptance could not be proven" },
                    { 6, "DEAD_LETTERED", "Dead-lettered", "Provider publication exhausted automatic submission" },
                    { 7, "MANUAL_RECONCILIATION", "Manual reconciliation", "Operator evidence is required before settlement" },
                    { 8, "ABANDONED", "Abandoned", "Provider publication was explicitly abandoned" }
                });

            migrationBuilder.Sql(
                """
                UPDATE webhook_messages
                SET payload_byte_length = octet_length(payload_bytes),
                    content_type = 'application/json',
                    content_encoding = 'utf-8',
                    occurred_at = created_at,
                    materialized_at = created_at
                WHERE payload_bytes IS NOT NULL;

                UPDATE incoming_webhook_messages
                SET payload_byte_length = octet_length(payload_bytes),
                    payload_provenance_id = 2,
                    content_type = 'application/json',
                    content_encoding = 'utf-8',
                    payload_retention_until = received_at + INTERVAL '14 days';
                """);

            migrationBuilder.CreateTable(
                name: "webhook_delivery_plan_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_consumer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_mode_id = table.Column<int>(type: "integer", nullable: false),
                    configuration_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_contract_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    retention_policy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    retention_policy_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_retention_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    materialized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_delivery_plan_snapshots", x => x.id);
                    table.UniqueConstraint("ak_webhook_delivery_plan_snapshots_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_webhook_delivery_plan_snapshots_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_delivery_plan_snapshots_webhook_consumers_tenant_id",
                        columns: x => new { x.tenant_id, x.webhook_consumer_id },
                        principalTable: "webhook_consumers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_delivery_plan_snapshots_webhook_messages_tenant_id_",
                        columns: x => new { x.tenant_id, x.webhook_message_id },
                        principalTable: "webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_delivery_plan_snapshots_webhook_provider_modes_prov",
                        column: x => x.provider_mode_id,
                        principalTable: "webhook_provider_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_local_target_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_plan_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint_configuration_version = table.Column<int>(type: "integer", nullable: false),
                    destination_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    credential_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    credential_version = table.Column<int>(type: "integer", nullable: false),
                    credential_valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    credential_valid_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: true),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivery_status_id = table.Column<int>(type: "integer", nullable: false),
                    next_action_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_lease_owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivery_fence = table.Column<long>(type: "bigint", nullable: false),
                    concurrency_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_local_target_snapshots", x => x.id);
                    table.UniqueConstraint("ak_webhook_local_target_snapshots_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_webhook_local_targets_concurrency_version", "concurrency_version > 0");
                    table.CheckConstraint("ck_webhook_local_targets_credential_version", "credential_version > 0");
                    table.CheckConstraint("ck_webhook_local_targets_delivery_fence", "delivery_fence >= 0");
                    table.CheckConstraint("ck_webhook_local_targets_endpoint_version", "endpoint_configuration_version > 0");
                    table.CheckConstraint("ck_webhook_local_targets_max_attempts", "max_attempts > 0");
                    table.CheckConstraint("ck_webhook_local_targets_timeout", "timeout_seconds > 0");
                    table.ForeignKey(
                        name: "fk_webhook_local_target_snapshots_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_local_target_snapshots_webhook_delivery_plan_snapsh",
                        columns: x => new { x.tenant_id, x.delivery_plan_snapshot_id },
                        principalTable: "webhook_delivery_plan_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_local_target_snapshots_webhook_endpoints_tenant_id_",
                        columns: x => new { x.tenant_id, x.webhook_endpoint_id },
                        principalTable: "webhook_endpoints",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_local_target_snapshots_webhook_local_delivery_statu",
                        column: x => x.delivery_status_id,
                        principalTable: "webhook_local_delivery_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_local_target_snapshots_webhook_messages_tenant_id_w",
                        columns: x => new { x.tenant_id, x.webhook_message_id },
                        principalTable: "webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_provider_publications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_delivery_plan_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_kind_id = table.Column<int>(type: "integer", nullable: false),
                    provider_binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(71)", maxLength: 71, nullable: false),
                    application_uid = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    provider_application_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    provider_environment = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    credential_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    credential_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mode_snapshot_id = table.Column<int>(type: "integer", nullable: false),
                    provider_configuration_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_contract_version = table.Column<int>(type: "integer", nullable: false),
                    retention_policy_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload_retention_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    publication_retention_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    idempotency_valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    external_provider_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    automatic_publication_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    automatic_reconciliation_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_automatic_reconciliation_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_action_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    safe_detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    processing_lease_owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    publication_fence = table.Column<long>(type: "bigint", nullable: false),
                    concurrency_version = table.Column<long>(type: "bigint", nullable: false),
                    prepared_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    publishing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    publication_unknown_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    manual_reconciliation_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    abandoned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_publications", x => x.id);
                    table.UniqueConstraint("ak_webhook_provider_publications_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_webhook_provider_publications_concurrency_version", "concurrency_version > 0");
                    table.CheckConstraint("ck_webhook_provider_publications_fence", "publication_fence >= 0");
                    table.CheckConstraint("ck_webhook_provider_publications_request_hash", "request_hash ~ '^sha256:[0-9a-f]{64}$'");
                    table.ForeignKey(
                        name: "fk_webhook_provider_publications_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_publications_webhook_consumer_provider_bin",
                        columns: x => new { x.tenant_id, x.provider_binding_id },
                        principalTable: "webhook_consumer_provider_bindings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_publications_webhook_delivery_plan_snapsho",
                        columns: x => new { x.tenant_id, x.webhook_delivery_plan_snapshot_id },
                        principalTable: "webhook_delivery_plan_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_publications_webhook_messages_tenant_id_we",
                        columns: x => new { x.tenant_id, x.webhook_message_id },
                        principalTable: "webhook_messages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_publications_webhook_provider_kinds_provid",
                        column: x => x.provider_kind_id,
                        principalTable: "webhook_provider_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_publications_webhook_provider_modes_mode_s",
                        column: x => x.mode_snapshot_id,
                        principalTable: "webhook_provider_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_publications_webhook_provider_publication_",
                        column: x => x.status_id,
                        principalTable: "webhook_provider_publication_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_provider_publication_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_provider_publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    publication_fence = table.Column<long>(type: "bigint", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    external_provider_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    safe_detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_publication_attempts", x => x.id);
                    table.UniqueConstraint("ak_webhook_provider_publication_attempts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_webhook_provider_publication_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_webhook_provider_publication_attempts_webhook_provider_publ",
                        columns: x => new { x.tenant_id, x.webhook_provider_publication_id },
                        principalTable: "webhook_provider_publications",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_messages_payload_provenance_id",
                table: "webhook_messages",
                column: "payload_provenance_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_messages_payload_byte_length",
                table: "webhook_messages",
                sql: "payload_byte_length > 0");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_status_id",
                table: "webhook_endpoints",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_status_tenant_id",
                table: "webhook_endpoints",
                columns: new[] { "tenant_id", "status_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_active_lease_caps",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "outcome_id", "processing_lease_expires_at", "endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_outcome_id",
                table: "webhook_delivery_attempts",
                column: "outcome_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_worker_poll",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "outcome_id", "scheduled_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_delivery_attempts_message_endpoint_attempt",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "message_id", "endpoint_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_consumer_kind_id",
                table: "webhook_consumers",
                column: "consumer_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_provider_mode_id",
                table: "webhook_consumers",
                column: "provider_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumers_status_id",
                table: "webhook_consumers",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_consumer_provider_bindings_provider_kind_id",
                table: "webhook_consumer_provider_bindings",
                column: "provider_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_claim_due",
                table: "incoming_webhook_messages",
                columns: new[] { "tenant_id", "status_id", "next_attempt_at", "processing_lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_payload_provenance_id",
                table: "incoming_webhook_messages",
                column: "payload_provenance_id");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_status_id",
                table: "incoming_webhook_messages",
                column: "status_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_incoming_webhook_messages_payload_byte_length",
                table: "incoming_webhook_messages",
                sql: "payload_byte_length > 0");

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_message_statuses_master_code",
                table: "incoming_webhook_message_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumer_kinds_master_code",
                table: "webhook_consumer_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_consumer_statuses_master_code",
                table: "webhook_consumer_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_delivery_attempt_outcomes_master_code",
                table: "webhook_delivery_attempt_outcomes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_plan_snapshots_provider_mode_id",
                table: "webhook_delivery_plan_snapshots",
                column: "provider_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_consumer_materialized",
                table: "webhook_delivery_plan_snapshots",
                columns: new[] { "tenant_id", "webhook_consumer_id", "materialized_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_plan_snapshots_tenant_retention",
                table: "webhook_delivery_plan_snapshots",
                columns: new[] { "tenant_id", "payload_retention_until_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_delivery_plan_snapshots_tenant_message",
                table: "webhook_delivery_plan_snapshots",
                columns: new[] { "tenant_id", "webhook_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_endpoint_statuses_master_code",
                table: "webhook_endpoint_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_local_delivery_statuses_master_code",
                table: "webhook_local_delivery_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_local_target_snapshots_delivery_status_id",
                table: "webhook_local_target_snapshots",
                column: "delivery_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_local_target_snapshots_tenant_id_webhook_endpoint_id",
                table: "webhook_local_target_snapshots",
                columns: new[] { "tenant_id", "webhook_endpoint_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_local_targets_tenant_claim_due",
                table: "webhook_local_target_snapshots",
                columns: new[] { "tenant_id", "delivery_status_id", "next_action_at_utc", "processing_lease_expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_local_targets_tenant_message",
                table: "webhook_local_target_snapshots",
                columns: new[] { "tenant_id", "webhook_message_id" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_local_targets_tenant_plan_endpoint",
                table: "webhook_local_target_snapshots",
                columns: new[] { "tenant_id", "delivery_plan_snapshot_id", "webhook_endpoint_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_payload_provenances_master_code",
                table: "webhook_payload_provenances",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_kinds_master_code",
                table: "webhook_provider_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_modes_master_code",
                table: "webhook_provider_modes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publication_attempts_tenant_recorded",
                table: "webhook_provider_publication_attempts",
                columns: new[] { "tenant_id", "recorded_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_publication_attempts_tenant_publication_attempt",
                table: "webhook_provider_publication_attempts",
                columns: new[] { "tenant_id", "webhook_provider_publication_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_publication_statuses_master_code",
                table: "webhook_provider_publication_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_mode_snapshot_id",
                table: "webhook_provider_publications",
                column: "mode_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_provider_kind_id",
                table: "webhook_provider_publications",
                column: "provider_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_status_id",
                table: "webhook_provider_publications",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_tenant_claim_due",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "status_id", "next_action_at", "processing_lease_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_tenant_id_provider_binding_id",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "provider_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_tenant_id_webhook_delivery_pl",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "webhook_delivery_plan_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publications_tenant_retention",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "publication_retention_until" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_publications_tenant_message_provider_binding",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "webhook_message_id", "provider_kind_id", "provider_binding_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_publications_tenant_provider_event",
                table: "webhook_provider_publications",
                columns: new[] { "tenant_id", "provider_kind_id", "provider_event_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_messages_incoming_webhook_message_statuses",
                table: "incoming_webhook_messages",
                column: "status_id",
                principalTable: "incoming_webhook_message_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_messages_webhook_payload_provenances_paylo",
                table: "incoming_webhook_messages",
                column: "payload_provenance_id",
                principalTable: "webhook_payload_provenances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumer_provider_bindings_webhook_provider_kinds_p",
                table: "webhook_consumer_provider_bindings",
                column: "provider_kind_id",
                principalTable: "webhook_provider_kinds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_webhook_consumer_kinds_consumer_kind_id",
                table: "webhook_consumers",
                column: "consumer_kind_id",
                principalTable: "webhook_consumer_kinds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_webhook_consumer_statuses_status_id",
                table: "webhook_consumers",
                column: "status_id",
                principalTable: "webhook_consumer_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_consumers_webhook_provider_modes_provider_mode_id",
                table: "webhook_consumers",
                column: "provider_mode_id",
                principalTable: "webhook_provider_modes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_delivery_attempt_outcomes",
                table: "webhook_delivery_attempts",
                column: "outcome_id",
                principalTable: "webhook_delivery_attempt_outcomes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_messages_tenant_id_messag",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "message_id" },
                principalTable: "webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_endpoints_webhook_endpoint_statuses_status_id",
                table: "webhook_endpoints",
                column: "status_id",
                principalTable: "webhook_endpoint_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_messages_webhook_payload_provenances_payload_proven",
                table: "webhook_messages",
                column: "payload_provenance_id",
                principalTable: "webhook_payload_provenances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_messages_incoming_webhook_message_statuses",
                table: "incoming_webhook_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_messages_webhook_payload_provenances_paylo",
                table: "incoming_webhook_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumer_provider_bindings_webhook_provider_kinds_p",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_webhook_consumer_kinds_consumer_kind_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_webhook_consumer_statuses_status_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_consumers_webhook_provider_modes_provider_mode_id",
                table: "webhook_consumers");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_delivery_attempt_outcomes",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_messages_tenant_id_messag",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_endpoints_webhook_endpoint_statuses_status_id",
                table: "webhook_endpoints");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_messages_webhook_payload_provenances_payload_proven",
                table: "webhook_messages");

            migrationBuilder.DropTable(
                name: "webhook_local_target_snapshots");

            migrationBuilder.DropTable(
                name: "webhook_provider_publication_attempts");

            migrationBuilder.DropTable(
                name: "webhook_provider_publications");

            migrationBuilder.DropTable(
                name: "webhook_delivery_plan_snapshots");

            migrationBuilder.DeleteData(
                table: "incoming_webhook_message_statuses",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            migrationBuilder.DeleteData(
                table: "webhook_consumer_kinds",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5 });

            migrationBuilder.DeleteData(
                table: "webhook_consumer_statuses",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3 });

            migrationBuilder.DeleteData(
                table: "webhook_delivery_attempt_outcomes",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5 });

            migrationBuilder.DeleteData(
                table: "webhook_endpoint_statuses",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4 });

            migrationBuilder.DeleteData(
                table: "webhook_local_delivery_statuses",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6 });

            migrationBuilder.DeleteData(
                table: "webhook_payload_provenances",
                keyColumn: "id",
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "webhook_provider_kinds",
                keyColumn: "id",
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "webhook_provider_modes",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5 });

            migrationBuilder.DeleteData(
                table: "webhook_provider_publication_statuses",
                keyColumn: "id",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            migrationBuilder.DropTable(
                name: "incoming_webhook_message_statuses");

            migrationBuilder.DropTable(
                name: "webhook_consumer_kinds");

            migrationBuilder.DropTable(
                name: "webhook_consumer_statuses");

            migrationBuilder.DropTable(
                name: "webhook_delivery_attempt_outcomes");

            migrationBuilder.DropTable(
                name: "webhook_endpoint_statuses");

            migrationBuilder.DropTable(
                name: "webhook_local_delivery_statuses");

            migrationBuilder.DropTable(
                name: "webhook_payload_provenances");

            migrationBuilder.DropTable(
                name: "webhook_provider_kinds");

            migrationBuilder.DropTable(
                name: "webhook_provider_publication_statuses");

            migrationBuilder.DropTable(
                name: "webhook_provider_modes");

            migrationBuilder.DropIndex(
                name: "ix_webhook_messages_payload_provenance_id",
                table: "webhook_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_messages_payload_byte_length",
                table: "webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoints_status_id",
                table: "webhook_endpoints");

            migrationBuilder.DropIndex(
                name: "ix_webhook_endpoints_status_tenant_id",
                table: "webhook_endpoints");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_webhook_delivery_attempts_tenant_id_id",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_active_lease_caps",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_outcome_id",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_attempts_worker_poll",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ux_webhook_delivery_attempts_message_endpoint_attempt",
                table: "webhook_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_consumer_kind_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_provider_mode_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumers_status_id",
                table: "webhook_consumers");

            migrationBuilder.DropIndex(
                name: "ix_webhook_consumer_provider_bindings_provider_kind_id",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_claim_due",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_payload_provenance_id",
                table: "incoming_webhook_messages");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_status_id",
                table: "incoming_webhook_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_incoming_webhook_messages_payload_byte_length",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "content_encoding",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "materialized_at",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "occurred_at",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_byte_length",
                table: "webhook_messages");

            migrationBuilder.DropColumn(
                name: "content_encoding",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_byte_length",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_cleared_at",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_provenance_id",
                table: "incoming_webhook_messages");

            migrationBuilder.DropColumn(
                name: "payload_retention_until",
                table: "incoming_webhook_messages");

            migrationBuilder.RenameColumn(
                name: "status_id",
                table: "webhook_endpoints",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "outcome_id",
                table: "webhook_delivery_attempts",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "status_id",
                table: "webhook_consumers",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "provider_mode_id",
                table: "webhook_consumers",
                newName: "provider_mode");

            migrationBuilder.RenameColumn(
                name: "consumer_kind_id",
                table: "webhook_consumers",
                newName: "consumer_kind");

            migrationBuilder.RenameColumn(
                name: "status_id",
                table: "incoming_webhook_messages",
                newName: "status");

            migrationBuilder.AlterColumn<byte[]>(
                name: "payload_bytes",
                table: "incoming_webhook_messages",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_endpoints_status_tenant_id",
                table: "webhook_endpoints",
                columns: new[] { "status", "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_attempts_active_lease_caps",
                table: "webhook_delivery_attempts",
                columns: new[] { "status", "processing_lease_expires_at", "tenant_id", "endpoint_id" });

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
                name: "ix_incoming_webhook_messages_claim_due",
                table: "incoming_webhook_messages",
                columns: new[] { "status", "next_attempt_at", "processing_lease_expires_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_delivery_attempts_webhook_messages_tenant_id_messag",
                table: "webhook_delivery_attempts",
                columns: new[] { "tenant_id", "message_id" },
                principalTable: "webhook_messages",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
