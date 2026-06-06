using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalFirstStorageFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_storage_objects_tenant_id",
                table: "storage_objects");

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                table: "storage_objects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "storage_objects",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "storage_objects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "storage_objects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "storage_objects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "lifecycle_state",
                table: "storage_objects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.AddColumn<string>(
                name: "object_key",
                table: "storage_objects",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owning_resource_id",
                table: "storage_objects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owning_resource_kind",
                table: "storage_objects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "storage_objects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "legacy_external");

            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "storage_objects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "legacy_image");

            migrationBuilder.AddColumn<string>(
                name: "quarantine_reason",
                table: "storage_objects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "quarantined_at",
                table: "storage_objects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "quarantined_by",
                table: "storage_objects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "safe_display_name",
                table: "storage_objects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sha256_checksum",
                table: "storage_objects",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "storage_objects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "public_image");

            migrationBuilder.Sql(
                """
                UPDATE storage_objects
                SET safe_display_name = COALESCE(NULLIF(full_name, ''), uri),
                    provider = 'legacy_external',
                    visibility = 'public_image',
                    purpose = 'legacy_image',
                    lifecycle_state = 'active'
                WHERE safe_display_name = ''
                   OR provider = ''
                   OR visibility = ''
                   OR purpose = ''
                   OR lifecycle_state = '';
                """);

            migrationBuilder.CreateTable(
                name: "storage_upload_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expected_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    reserved_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    safe_display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    extension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    purpose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    object_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    sha256_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    storage_object_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    upload_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storage_upload_sessions", x => x.id);
                    table.CheckConstraint("ck_storage_upload_sessions_expected_size_nonnegative", "expected_size_bytes >= 0");
                    table.CheckConstraint("ck_storage_upload_sessions_provider", "provider IN ('local', 's3_compatible', 'legacy_external')");
                    table.CheckConstraint("ck_storage_upload_sessions_purpose", "purpose IN ('legacy_image', 'profile_image', 'event_image', 'attachment', 'document', 'system_asset')");
                    table.CheckConstraint("ck_storage_upload_sessions_reserved_bytes_nonnegative", "reserved_bytes >= 0");
                    table.CheckConstraint("ck_storage_upload_sessions_status", "status IN ('reserved', 'uploading', 'finalized', 'canceled', 'failed', 'expired')");
                    table.CheckConstraint("ck_storage_upload_sessions_visibility", "visibility IN ('public_image', 'authenticated_tenant', 'private_owner')");
                    table.ForeignKey(
                        name: "fk_storage_upload_sessions_storage_objects_storage_object_id",
                        column: x => x.storage_object_id,
                        principalTable: "storage_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_storage_upload_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_storage_upload_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "storage_usage_counters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    used_bytes = table.Column<long>(type: "bigint", nullable: false),
                    reserved_bytes = table.Column<long>(type: "bigint", nullable: false),
                    quarantined_bytes = table.Column<long>(type: "bigint", nullable: false),
                    object_count = table.Column<long>(type: "bigint", nullable: false),
                    last_recalculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storage_usage_counters", x => x.id);
                    table.CheckConstraint("ck_storage_usage_counters_object_count_nonnegative", "object_count >= 0");
                    table.CheckConstraint("ck_storage_usage_counters_provider", "provider IN ('local', 's3_compatible', 'legacy_external')");
                    table.CheckConstraint("ck_storage_usage_counters_quarantined_bytes_nonnegative", "quarantined_bytes >= 0");
                    table.CheckConstraint("ck_storage_usage_counters_reserved_bytes_nonnegative", "reserved_bytes >= 0");
                    table.CheckConstraint("ck_storage_usage_counters_used_bytes_nonnegative", "used_bytes >= 0");
                    table.ForeignKey(
                        name: "fk_storage_usage_counters_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_storage_objects_tenant_owner",
                table: "storage_objects",
                columns: new[] { "tenant_id", "owning_resource_kind", "owning_resource_id" },
                filter: "owning_resource_kind IS NOT NULL AND owning_resource_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_storage_objects_tenant_provider_lifecycle_state",
                table: "storage_objects",
                columns: new[] { "tenant_id", "provider", "lifecycle_state" });

            migrationBuilder.CreateIndex(
                name: "ix_storage_objects_tenant_visibility_purpose",
                table: "storage_objects",
                columns: new[] { "tenant_id", "visibility", "purpose" });

            migrationBuilder.CreateIndex(
                name: "ux_storage_objects_provider_object_key",
                table: "storage_objects",
                columns: new[] { "provider", "object_key" },
                unique: true,
                filter: "object_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_objects_lifecycle_state",
                table: "storage_objects",
                sql: "lifecycle_state IN ('pending', 'active', 'quarantined', 'delete_requested', 'deleted')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_objects_provider",
                table: "storage_objects",
                sql: "provider IN ('local', 's3_compatible', 'legacy_external')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_objects_purpose",
                table: "storage_objects",
                sql: "purpose IN ('legacy_image', 'profile_image', 'event_image', 'attachment', 'document', 'system_asset')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_objects_size_nonnegative",
                table: "storage_objects",
                sql: "size >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_objects_visibility",
                table: "storage_objects",
                sql: "visibility IN ('public_image', 'authenticated_tenant', 'private_owner')");

            migrationBuilder.CreateIndex(
                name: "ix_storage_upload_sessions_provider_object_key",
                table: "storage_upload_sessions",
                columns: new[] { "provider", "object_key" },
                filter: "object_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_storage_upload_sessions_storage_object_id",
                table: "storage_upload_sessions",
                column: "storage_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_upload_sessions_tenant_status_expires_at",
                table: "storage_upload_sessions",
                columns: new[] { "tenant_id", "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_storage_upload_sessions_user_id",
                table: "storage_upload_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_storage_upload_sessions_tenant_idempotency_key",
                table: "storage_upload_sessions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_storage_usage_counters_tenant_provider",
                table: "storage_usage_counters",
                columns: new[] { "tenant_id", "provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storage_upload_sessions");

            migrationBuilder.DropTable(
                name: "storage_usage_counters");

            migrationBuilder.DropIndex(
                name: "ix_storage_objects_tenant_owner",
                table: "storage_objects");

            migrationBuilder.DropIndex(
                name: "ix_storage_objects_tenant_provider_lifecycle_state",
                table: "storage_objects");

            migrationBuilder.DropIndex(
                name: "ix_storage_objects_tenant_visibility_purpose",
                table: "storage_objects");

            migrationBuilder.DropIndex(
                name: "ux_storage_objects_provider_object_key",
                table: "storage_objects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_objects_lifecycle_state",
                table: "storage_objects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_objects_provider",
                table: "storage_objects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_objects_purpose",
                table: "storage_objects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_objects_size_nonnegative",
                table: "storage_objects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_objects_visibility",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "lifecycle_state",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "object_key",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "owning_resource_id",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "owning_resource_kind",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "quarantine_reason",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "quarantined_at",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "quarantined_by",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "safe_display_name",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "sha256_checksum",
                table: "storage_objects");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "storage_objects");

            migrationBuilder.CreateIndex(
                name: "ix_storage_objects_tenant_id",
                table: "storage_objects",
                column: "tenant_id");
        }
    }
}
