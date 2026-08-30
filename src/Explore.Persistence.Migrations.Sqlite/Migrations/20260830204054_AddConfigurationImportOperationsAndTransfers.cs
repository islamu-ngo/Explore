using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationImportOperationsAndTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings");

            migrationBuilder.DropColumn(
                name: "inline_ciphertext",
                table: "ie_secret_bindings");

            migrationBuilder.DropColumn(
                name: "inline_ciphertext_version",
                table: "ie_secret_bindings");

            migrationBuilder.AddColumn<Guid>(
                name: "add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "add_on_total_minor_snapshot",
                table: "ie_registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_directory_operator_document_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "connect_platform_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "external_account_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "merchant_country_code",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "operator_kind_code",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "operator_legal_name",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "operator_registration_identifier",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organizer_payment_provider_connection_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ie_configuration_direct_transfer_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    source_authority = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    target_authority_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    destination_origin_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    destination_proof_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    nonce_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_byte_length = table.Column<int>(type: "INTEGER", nullable: false),
                    next_offset = table.Column<int>(type: "INTEGER", nullable: false),
                    last_chunk_offset = table.Column<int>(type: "INTEGER", nullable: false),
                    last_chunk_byte_length = table.Column<int>(type: "INTEGER", nullable: false),
                    last_chunk_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    source_approved_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    destination_approved_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_direct_transfer_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_configuration_import_artifacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    protected_payload = table.Column<byte[]>(type: "BLOB", maxLength: 4210688, nullable: false),
                    sha256digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    byte_length = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_import_artifacts", x => x.id);
                    table.CheckConstraint("ck_configuration_import_artifacts_byte_length", "byte_length BETWEEN 1 AND 4194304");
                });

            migrationBuilder.CreateTable(
                name: "ie_configuration_import_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    session_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    kind = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    target_authority_key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    source_operation_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    artifact_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    target_revision_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    selected_sections_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    mapping_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    approval_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    apply_mode = table.Column<int>(type: "INTEGER", nullable: false),
                    snapshot_artifact_handle_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    snapshot_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    snapshot_expires_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    effect_outbox_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    fidelity_verified = table.Column<bool>(type: "INTEGER", nullable: false),
                    fidelity_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    failure_code = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    failure_reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    omitted_section_keys = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    selected_section_keys = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_import_operations", x => x.id);
                    table.CheckConstraint("ck_configuration_import_operations_kind", "kind BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_configuration_import_operations_status", "status BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_configuration_import_operations_target", "((target_authority_key = 'instance' AND target_tenant_id IS NULL) OR (target_authority_key <> 'instance' AND target_tenant_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_configuration_import_operations_configuration_import_operations_source_operation_id",
                        column: x => x.source_operation_id,
                        principalTable: "ie_configuration_import_operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_configuration_import_sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    target_scope = table.Column<int>(type: "INTEGER", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    target_authority_key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    artifact_handle_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    artifact_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_byte_length = table.Column<int>(type: "INTEGER", nullable: false),
                    artifact_expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    access_token_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    consumed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    preview_artifact_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    preview_target_revision_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    preview_selected_sections_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    preview_mapping_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    preview_required_approval_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    preview_apply_mode = table.Column<int>(type: "INTEGER", nullable: true),
                    preview_expires_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_import_sessions", x => x.session_id);
                    table.CheckConstraint("ck_configuration_import_sessions_artifact_length", "artifact_byte_length BETWEEN 1 AND 4194304");
                    table.CheckConstraint("ck_configuration_import_sessions_state", "state BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_configuration_import_sessions_target", "((target_scope = 1 AND target_tenant_id IS NULL) OR (target_scope = 2 AND target_tenant_id IS NOT NULL))");
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_catalog_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    version_number = table.Column<int>(type: "INTEGER", nullable: false),
                    published_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    retired_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_catalog_versions", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_catalog_versions_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_add_on_catalog_versions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_catalog_versions_lifecycle", "retired_at IS NULL OR (published_at IS NOT NULL AND retired_at >= published_at)");
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_versions_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_legal_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    scope = table.Column<int>(type: "INTEGER", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    authority_key = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    kind = table.Column<int>(type: "INTEGER", nullable: false),
                    owner_role = table.Column<int>(type: "INTEGER", nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    current_version = table.Column<int>(type: "INTEGER", nullable: false),
                    accountable_identity_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_documents", x => x.id);
                    table.CheckConstraint("ck_legal_documents_current_version", "current_version > 0");
                    table.CheckConstraint("ck_legal_documents_scope_tenant", "(scope = 1 AND tenant_id IS NULL) OR (scope = 2 AND tenant_id IS NOT NULL)");
                    table.CheckConstraint("ck_legal_documents_state", "state >= 1 AND state <= 6");
                });

            migrationBuilder.CreateTable(
                name: "ie_ticketing_recovery_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recovery_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    manifest_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    release_revision = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    schema_revision = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    database_checkpoint = table.Column<long>(type: "INTEGER", nullable: false),
                    object_cutoff_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    retained_key_version = table.Column<int>(type: "INTEGER", nullable: false),
                    authority_floor = table.Column<long>(type: "INTEGER", nullable: false),
                    provider_cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    idempotency_floor = table.Column<long>(type: "INTEGER", nullable: false),
                    worker_fence = table.Column<long>(type: "INTEGER", nullable: false),
                    capability_generation = table.Column<int>(type: "INTEGER", nullable: false),
                    credential_generation = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    validated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    authority_rotated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    workers_opened_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    sales_opened_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    failure_code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_checkpoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_ticketing_recovery_reissue_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recovery_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    required_credential_generation = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_reissue_intents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_configuration_direct_transfer_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    session_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    offset = table.Column<int>(type: "INTEGER", nullable: false),
                    byte_length = table.Column<int>(type: "INTEGER", nullable: false),
                    digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    protected_payload = table.Column<byte[]>(type: "BLOB", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_direct_transfer_chunks", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuration_direct_transfer_chunks_configuration_direct_transfer_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "ie_configuration_direct_transfer_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_catalog_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    unit_price_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    inventory_capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    fulfillment_disclosure = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    refund_disclosure = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_catalog_items", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_id", x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.id });
                    table.UniqueConstraint("ak_event_add_on_catalog_items_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_catalog_items_capacity", "inventory_capacity > 0");
                    table.CheckConstraint("ck_event_add_on_catalog_items_money", "unit_price_minor >= 0");
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_items_event_add_on_catalog_versions_tenant_id_event_add_on_catalog_version_id",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_legal_document_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    audience = table.Column<int>(type: "INTEGER", nullable: false),
                    state = table.Column<int>(type: "INTEGER", nullable: false),
                    content_digest = table.Column<string>(type: "TEXT", unicode: false, maxLength: 64, nullable: false),
                    source_origin = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    requires_fresh_acceptance = table.Column<bool>(type: "INTEGER", nullable: false),
                    template_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    template_version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    template_source_kind = table.Column<int>(type: "INTEGER", nullable: true),
                    template_license_expression = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    template_review_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    reviewer_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    review_evidence_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    accountable_identity_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    approved_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    proposed_effective_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    published_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    retired_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_versions", x => x.id);
                    table.CheckConstraint("ck_legal_document_versions_state", "state >= 1 AND state <= 6");
                    table.CheckConstraint("ck_legal_document_versions_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_legal_document_versions_legal_documents_legal_document_id",
                        column: x => x.legal_document_id,
                        principalTable: "ie_legal_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_registration_order_add_on_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    name_snapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    unit_price_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    line_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    currency_code_snapshot = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    fulfillment_disclosure_snapshot = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    refund_disclosure_snapshot = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_order_add_on_lines", x => x.id);
                    table.UniqueConstraint("ak_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_id", x => new { x.tenant_id, x.event_id, x.registration_order_id, x.id });
                    table.UniqueConstraint("ak_registration_order_add_on_lines_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_order_add_on_lines_money", "unit_price_minor_snapshot >= 0 AND line_total_minor_snapshot >= 0");
                    table.CheckConstraint("ck_registration_order_add_on_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_event_add_o_ae0eaf0b7f25",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_event_add_on_catalog_versions_tenant_id_event_id_event_add_on_catalog_version_id",
                        columns: x => new { x.tenant_id, x.event_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_registration_orders_tenant_id_event_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_order_add_on_lines_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_legal_document_localized_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    language_tag = table.Column<string>(type: "TEXT", unicode: false, maxLength: 35, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    markdown = table.Column<string>(type: "TEXT", maxLength: 262144, nullable: false),
                    utf8_byte_count = table.Column<int>(type: "INTEGER", nullable: false),
                    link_count = table.Column<int>(type: "INTEGER", nullable: false),
                    placeholder_count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_localized_sources", x => x.id);
                    table.CheckConstraint("ck_legal_document_localized_sources_counts", "utf8_byte_count >= 1 AND utf8_byte_count <= 262144 AND link_count >= 0 AND link_count <= 128 AND placeholder_count >= 0 AND placeholder_count <= 64");
                    table.ForeignKey(
                        name: "fk_legal_document_localized_sources_legal_document_versions_legal_document_version_id",
                        column: x => x.legal_document_version_id,
                        principalTable: "ie_legal_document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_legal_document_publications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    legal_document_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    lifecycle_state = table.Column<int>(type: "INTEGER", nullable: false),
                    content_digest = table.Column<string>(type: "TEXT", unicode: false, maxLength: 64, nullable: false),
                    accountable_identity_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    review_evidence_reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    effective_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    requires_fresh_acceptance = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_publications", x => x.id);
                    table.CheckConstraint("ck_legal_document_publications_state", "lifecycle_state IN (5, 6)");
                    table.CheckConstraint("ck_legal_document_publications_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_legal_document_publications_legal_document_versions_legal_document_version_id",
                        column: x => x.legal_document_version_id,
                        principalTable: "ie_legal_document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_legal_document_publications_legal_documents_legal_document_id",
                        column: x => x.legal_document_id,
                        principalTable: "ie_legal_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_fulfillments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    fulfilled_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_fulfillments", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_fulfillments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_add_on_fulfillments_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_registration__2813f39d06a6",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_fulfillments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_inventory_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    released_quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    active_uniqueness_slot = table.Column<Guid>(type: "TEXT", nullable: true),
                    reserved_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    released_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_inventory_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_inventory_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_inventory_allocations_quantity", "quantity > 0 AND released_quantity >= 0 AND released_quantity <= quantity");
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_event_add_on_catalog_items_tenant_id_event_add_on_catalog_item_id",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_regi_5156e1858f3f",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_refund_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    refund_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    allocated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    confirmed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    failed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_refund_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_refund_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_refund_allocations_money", "amount_minor >= 0");
                    table.CheckConstraint("ck_event_add_on_refund_allocations_quantity", "quantity > 0");
                    table.CheckConstraint("ck_event_add_on_refund_allocations_status", "status >= 1 AND status <= 4");
                    table.ForeignKey(
                        name: "fk_event_add_on_refund_allocations_refund_attempts_tenant_id_refund_operation_id",
                        columns: x => new { x.tenant_id, x.refund_operation_id },
                        principalTable: "ie_refund_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_refund_allocations_registration_order_add_on_lines_tenant_id_event_id_registration_order_id_registr_d4eabd70ae6e",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_add_on_refund_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL) OR (secret_source_type_id = 1 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_registration_orders_tenant_id_event_id_add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_chunks_session_id_offset",
                table: "ie_configuration_direct_transfer_chunks",
                columns: new[] { "session_id", "offset" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_sessions_destination_proof_digest",
                table: "ie_configuration_direct_transfer_sessions",
                column: "destination_proof_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_sessions_nonce_digest",
                table: "ie_configuration_direct_transfer_sessions",
                column: "nonce_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_sessions_target_authority_key_created_at",
                table: "ie_configuration_direct_transfer_sessions",
                columns: new[] { "target_authority_key", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_artifacts_expires_at",
                table: "ie_configuration_import_artifacts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_session_id",
                table: "ie_configuration_import_operations",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_snapshot_artifact_handle_id",
                table: "ie_configuration_import_operations",
                column: "snapshot_artifact_handle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_source_operation_id",
                table: "ie_configuration_import_operations",
                column: "source_operation_id");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_target_authority_key_started_at",
                table: "ie_configuration_import_operations",
                columns: new[] { "target_authority_key", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_sessions_artifact_handle_id",
                table: "ie_configuration_import_sessions",
                column: "artifact_handle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_sessions_target_authority_key_state_expires_at",
                table: "ie_configuration_import_sessions",
                columns: new[] { "target_authority_key", "state", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_id",
                table: "ie_event_add_on_catalog_items",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_items_tenant_id_event_add_on_catalog_version_id_name",
                table: "ie_event_add_on_catalog_items",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_versions_tenant_id_event_id",
                table: "ie_event_add_on_catalog_versions",
                columns: new[] { "tenant_id", "event_id" },
                unique: true,
                filter: "published_at IS NOT NULL AND retired_at IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_catalog_versions_tenant_id_event_id_version_number",
                table: "ie_event_add_on_catalog_versions",
                columns: new[] { "tenant_id", "event_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_event_id_registration_order_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_operation_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_event_add_on_catalog_item_id_released_at",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_add_on_catalog_item_id", "released_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_event_id_registration_order_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_operation_id",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_registration_order_add_on_line_id_active_uniqueness_slot",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id", "active_uniqueness_slot" },
                unique: true,
                filter: "active_uniqueness_slot IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_refund_allocations_tenant_id_event_id_registration_order_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_refund_allocations_tenant_id_refund_operation_id",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "refund_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_refund_allocations_tenant_id_registration_order_add_on_line_id",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_localized_sources_legal_document_version_id_language_tag",
                table: "ie_legal_document_localized_sources",
                columns: new[] { "legal_document_version_id", "language_tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_publications_legal_document_id_occurred_at",
                table: "ie_legal_document_publications",
                columns: new[] { "legal_document_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_publications_legal_document_id_version_lifecycle_state",
                table: "ie_legal_document_publications",
                columns: new[] { "legal_document_id", "version", "lifecycle_state" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_publications_legal_document_version_id",
                table: "ie_legal_document_publications",
                column: "legal_document_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_versions_legal_document_id_version",
                table: "ie_legal_document_versions",
                columns: new[] { "legal_document_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_versions_state_proposed_effective_at",
                table: "ie_legal_document_versions",
                columns: new[] { "state", "proposed_effective_at" });

            migrationBuilder.CreateIndex(
                name: "ix_legal_documents_authority_key_kind",
                table: "ie_legal_documents",
                columns: new[] { "authority_key", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_documents_tenant_id_state_kind",
                table: "ie_legal_documents",
                columns: new[] { "tenant_id", "state", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_add_on_lines_tenant_id_event_add_on_catalog_version_id_event_add_on_catalog_item_id",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "event_add_on_catalog_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_add_on_lines_tenant_id_event_id_event_add_on_catalog_version_id",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_id", "event_add_on_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_order_add_on_lines_tenant_id_registration_order_id_event_add_on_catalog_item_id",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "registration_order_id", "event_add_on_catalog_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_recovery_operation_id",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "recovery_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_status",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_reissue_intents_tenant_id_recovery_operation_id_admission_ticket_id",
                table: "ie_ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "recovery_operation_id", "admission_ticket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_reissue_intents_tenant_id_status",
                table: "ie_ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "fk_registration_orders_event_add_on_catalog_versions_tenant_id_event_id_add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" },
                principalTable: "ie_event_add_on_catalog_versions",
                principalColumns: new[] { "tenant_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_orders_event_add_on_catalog_versions_tenant_id_event_id_add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropTable(
                name: "ie_configuration_direct_transfer_chunks");

            migrationBuilder.DropTable(
                name: "ie_configuration_import_artifacts");

            migrationBuilder.DropTable(
                name: "ie_configuration_import_operations");

            migrationBuilder.DropTable(
                name: "ie_configuration_import_sessions");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_fulfillments");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_inventory_allocations");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_refund_allocations");

            migrationBuilder.DropTable(
                name: "ie_legal_document_localized_sources");

            migrationBuilder.DropTable(
                name: "ie_legal_document_publications");

            migrationBuilder.DropTable(
                name: "ie_ticketing_recovery_checkpoints");

            migrationBuilder.DropTable(
                name: "ie_ticketing_recovery_reissue_intents");

            migrationBuilder.DropTable(
                name: "ie_configuration_direct_transfer_sessions");

            migrationBuilder.DropTable(
                name: "ie_registration_order_add_on_lines");

            migrationBuilder.DropTable(
                name: "ie_legal_document_versions");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_catalog_items");

            migrationBuilder.DropTable(
                name: "ie_legal_documents");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_catalog_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings");

            migrationBuilder.DropIndex(
                name: "ix_registration_orders_tenant_id_event_id_add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "add_on_total_minor_snapshot",
                table: "ie_registration_orders");

            migrationBuilder.DropColumn(
                name: "connect_platform_id",
                table: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.DropColumn(
                name: "external_account_id",
                table: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.DropColumn(
                name: "merchant_country_code",
                table: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.DropColumn(
                name: "operator_kind_code",
                table: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.DropColumn(
                name: "operator_legal_name",
                table: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.DropColumn(
                name: "operator_registration_identifier",
                table: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.DropColumn(
                name: "organizer_payment_provider_connection_id",
                table: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.AddColumn<byte[]>(
                name: "inline_ciphertext",
                table: "ie_secret_bindings",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inline_ciphertext_version",
                table: "ie_secret_bindings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_directory_operator_document_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (secret_source_type_id = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (secret_source_type_id = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
        }
    }
}
