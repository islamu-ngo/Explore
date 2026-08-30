using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddPortableLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "add_on_catalog_version_id_snapshot",
                table: "ie_registration_orders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<long>(
                name: "add_on_total_minor_snapshot",
                table: "ie_registration_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_directory_operator_document_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "connect_platform_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "external_account_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "merchant_country_code",
                table: "ie_paid_order_acceptance_snapshots",
                type: "varchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "operator_kind_code",
                table: "ie_paid_order_acceptance_snapshots",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "operator_legal_name",
                table: "ie_paid_order_acceptance_snapshots",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "operator_registration_identifier",
                table: "ie_paid_order_acceptance_snapshots",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "organizer_payment_provider_connection_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_catalog_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    retired_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_legal_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    scope = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    authority_key = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<int>(type: "int", nullable: false),
                    owner_role = table.Column<int>(type: "int", nullable: false),
                    state = table.Column<int>(type: "int", nullable: false),
                    current_version = table.Column<int>(type: "int", nullable: false),
                    accountable_identity_reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_documents", x => x.id);
                    table.CheckConstraint("ck_legal_documents_current_version", "current_version > 0");
                    table.CheckConstraint("ck_legal_documents_scope_tenant", "(scope = 1 AND tenant_id IS NULL) OR (scope = 2 AND tenant_id IS NOT NULL)");
                    table.CheckConstraint("ck_legal_documents_state", "state >= 1 AND state <= 6");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_ticketing_recovery_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    recovery_operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    manifest_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    release_revision = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    schema_revision = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    database_checkpoint = table.Column<long>(type: "bigint", nullable: false),
                    object_cutoff_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    retained_key_version = table.Column<int>(type: "int", nullable: false),
                    authority_floor = table.Column<long>(type: "bigint", nullable: false),
                    provider_cursor = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_floor = table.Column<long>(type: "bigint", nullable: false),
                    worker_fence = table.Column<long>(type: "bigint", nullable: false),
                    capability_generation = table.Column<int>(type: "int", nullable: false),
                    credential_generation = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    validated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    authority_rotated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    workers_opened_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sales_opened_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failure_code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_checkpoints", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_ticketing_recovery_reissue_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    recovery_operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    required_credential_generation = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_reissue_intents", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_catalog_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit_price_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    inventory_capacity = table.Column<int>(type: "int", nullable: false),
                    fulfillment_disclosure = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_disclosure = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_catalog_items", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_catalog_items_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.UniqueConstraint("ak_ie_event_add_on_catalog_items_tenant_id_event_add_on_448129aa", x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.id });
                    table.CheckConstraint("ck_event_add_on_catalog_items_capacity", "inventory_capacity > 0");
                    table.CheckConstraint("ck_event_add_on_catalog_items_money", "unit_price_minor >= 0");
                    table.ForeignKey(
                        name: "fk_event_add_on_catalog_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_catalog_items_ie_event_add_on_catalo_05a3d12c",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_legal_document_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    legal_document_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    version = table.Column<int>(type: "int", nullable: false),
                    audience = table.Column<int>(type: "int", nullable: false),
                    state = table.Column<int>(type: "int", nullable: false),
                    content_digest = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_origin = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requires_fresh_acceptance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    template_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    template_version = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    template_source_kind = table.Column<int>(type: "int", nullable: true),
                    template_license_expression = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    template_review_reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewer_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    review_evidence_reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accountable_identity_reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    proposed_effective_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    published_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    retired_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_versions", x => x.id);
                    table.CheckConstraint("ck_legal_document_versions_state", "state >= 1 AND state <= 6");
                    table.CheckConstraint("ck_legal_document_versions_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_ie_legal_document_versions_ie_legal_documents_legal__5ca7f0d4",
                        column: x => x.legal_document_id,
                        principalTable: "ie_legal_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_registration_order_add_on_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    name_snapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit_price_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    line_total_minor_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    currency_code_snapshot = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fulfillment_disclosure_snapshot = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_disclosure_snapshot = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_order_add_on_lines", x => x.id);
                    table.UniqueConstraint("ak_ie_registration_order_add_on_lines_tenant_id_event_i_2a6a2928", x => new { x.tenant_id, x.event_id, x.registration_order_id, x.id });
                    table.UniqueConstraint("ak_registration_order_add_on_lines_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_order_add_on_lines_money", "unit_price_minor_snapshot >= 0 AND line_total_minor_snapshot >= 0");
                    table.CheckConstraint("ck_registration_order_add_on_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_ie_registration_order_add_on_lines_ie_event_add_on_c_cb43a004",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_version_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_registration_order_add_on_lines_ie_event_add_on_c_cec0f4ba",
                        columns: x => new { x.tenant_id, x.event_id, x.event_add_on_catalog_version_id },
                        principalTable: "ie_event_add_on_catalog_versions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_registration_order_add_on_lines_ie_registration_o_efb429c6",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_legal_document_localized_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    legal_document_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    language_tag = table.Column<string>(type: "varchar(35)", unicode: false, maxLength: 35, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    summary = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    markdown = table.Column<string>(type: "longtext", maxLength: 262144, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    utf8_byte_count = table.Column<int>(type: "int", nullable: false),
                    link_count = table.Column<int>(type: "int", nullable: false),
                    placeholder_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_localized_sources", x => x.id);
                    table.CheckConstraint("ck_legal_document_localized_sources_counts", "utf8_byte_count >= 1 AND utf8_byte_count <= 262144 AND link_count >= 0 AND link_count <= 128 AND placeholder_count >= 0 AND placeholder_count <= 64");
                    table.ForeignKey(
                        name: "fk_ie_legal_document_localized_sources_ie_legal_documen_6e5d3dd1",
                        column: x => x.legal_document_version_id,
                        principalTable: "ie_legal_document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_legal_document_publications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    legal_document_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    legal_document_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    version = table.Column<int>(type: "int", nullable: false),
                    lifecycle_state = table.Column<int>(type: "int", nullable: false),
                    content_digest = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accountable_identity_reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    review_evidence_reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    effective_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    requires_fresh_acceptance = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_legal_document_publications", x => x.id);
                    table.CheckConstraint("ck_legal_document_publications_state", "lifecycle_state IN (5, 6)");
                    table.CheckConstraint("ck_legal_document_publications_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_ie_legal_document_publications_ie_legal_document_ver_59dac480",
                        column: x => x.legal_document_version_id,
                        principalTable: "ie_legal_document_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_legal_document_publications_ie_legal_documents_le_615b7a87",
                        column: x => x.legal_document_id,
                        principalTable: "ie_legal_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_fulfillments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    fulfilled_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_fulfillments", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_fulfillments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_add_on_fulfillments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_fulfillments_ie_registration_order_a_69f823e2",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_inventory_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_add_on_catalog_item_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    released_quantity = table.Column<int>(type: "int", nullable: false),
                    active_uniqueness_slot = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    reserved_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    released_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_inventory_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_inventory_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_inventory_allocations_quantity", "quantity > 0 AND released_quantity >= 0 AND released_quantity <= quantity");
                    table.ForeignKey(
                        name: "fk_event_add_on_inventory_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_inventory_allocations_ie_event_add_o_6fab57c5",
                        columns: x => new { x.tenant_id, x.event_add_on_catalog_item_id },
                        principalTable: "ie_event_add_on_catalog_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_inventory_allocations_ie_registratio_1f353da3",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_event_add_on_refund_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    refund_operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_add_on_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allocated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    confirmed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_event_add_on_refund_allocations", x => x.id);
                    table.UniqueConstraint("ak_event_add_on_refund_allocations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_add_on_refund_allocations_money", "amount_minor >= 0");
                    table.CheckConstraint("ck_event_add_on_refund_allocations_quantity", "quantity > 0");
                    table.CheckConstraint("ck_event_add_on_refund_allocations_status", "status >= 1 AND status <= 4");
                    table.ForeignKey(
                        name: "fk_event_add_on_refund_allocations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_refund_allocations_ie_refund_attempt_0be83854",
                        columns: x => new { x.tenant_id, x.refund_operation_id },
                        principalTable: "ie_refund_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_event_add_on_refund_allocations_ie_registration_o_b166ff79",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_order_add_on_line_id },
                        principalTable: "ie_registration_order_add_on_lines",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_orders_tenant_id_event_id_add_on_cat_6a2d9031",
                table: "ie_registration_orders",
                columns: new[] { "tenant_id", "event_id", "add_on_catalog_version_id_snapshot" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_catalog_items_tenant_id_event_add_on_31d30e11",
                table: "ie_event_add_on_catalog_items",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_catalog_items_tenant_id_event_add_on_3c1a9f32",
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
                name: "ix_ie_event_add_on_catalog_versions_tenant_id_event_id__2953ee09",
                table: "ie_event_add_on_catalog_versions",
                columns: new[] { "tenant_id", "event_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_fulfillments_tenant_id_operation_id",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_fulfillments_tenant_id_event_id_regi_eb07ddbb",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_fulfillments_tenant_id_registration__3e46ee7a",
                table: "ie_event_add_on_fulfillments",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_add_on_inventory_allocations_tenant_id_operation_id",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_inventory_allocations_tenant_id_even_2d9802a1",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_add_on_catalog_item_id", "released_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_inventory_allocations_tenant_id_even_ee418489",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_inventory_allocations_tenant_id_regi_59a7a7f2",
                table: "ie_event_add_on_inventory_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id", "active_uniqueness_slot" },
                unique: true,
                filter: "active_uniqueness_slot IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_refund_allocations_tenant_id_event_i_5cc5cd44",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_refund_allocations_tenant_id_refund__60be1706",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "refund_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_event_add_on_refund_allocations_tenant_id_registr_8437c3f3",
                table: "ie_event_add_on_refund_allocations",
                columns: new[] { "tenant_id", "registration_order_add_on_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_legal_document_localized_sources_legal_document_v_d20f8079",
                table: "ie_legal_document_localized_sources",
                columns: new[] { "legal_document_version_id", "language_tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_legal_document_publications_legal_document_id_ver_78e4bf63",
                table: "ie_legal_document_publications",
                columns: new[] { "legal_document_id", "version", "lifecycle_state" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_document_publications_legal_document_id_occurred_at",
                table: "ie_legal_document_publications",
                columns: new[] { "legal_document_id", "occurred_at" });

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
                name: "ix_ie_registration_order_add_on_lines_tenant_id_event_a_f6f2a17b",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_add_on_catalog_version_id", "event_add_on_catalog_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_order_add_on_lines_tenant_id_event_i_e2fd4950",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "event_id", "event_add_on_catalog_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_order_add_on_lines_tenant_id_registr_774eb183",
                table: "ie_registration_order_add_on_lines",
                columns: new[] { "tenant_id", "registration_order_id", "event_add_on_catalog_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticketing_recovery_checkpoints_tenant_id_recovery_c44e37e9",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "recovery_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_status",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticketing_recovery_reissue_intents_tenant_id_reco_a6bb0d1d",
                table: "ie_ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "recovery_operation_id", "admission_ticket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_reissue_intents_tenant_id_status",
                table: "ie_ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "fk_ie_registration_orders_ie_event_add_on_catalog_versi_58105e19",
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
                name: "fk_ie_registration_orders_ie_event_add_on_catalog_versi_58105e19",
                table: "ie_registration_orders");

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
                name: "ie_registration_order_add_on_lines");

            migrationBuilder.DropTable(
                name: "ie_legal_document_versions");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_catalog_items");

            migrationBuilder.DropTable(
                name: "ie_legal_documents");

            migrationBuilder.DropTable(
                name: "ie_event_add_on_catalog_versions");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_orders_tenant_id_event_id_add_on_cat_6a2d9031",
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

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_directory_operator_document_id",
                table: "ie_paid_order_acceptance_snapshots",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");
        }
    }
}
