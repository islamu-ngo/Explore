using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationManifestPortability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                schema: "islamu_event",
                table: "secret_bindings");

            migrationBuilder.DropColumn(
                name: "inline_ciphertext",
                schema: "islamu_event",
                table: "secret_bindings");

            migrationBuilder.DropColumn(
                name: "inline_ciphertext_version",
                schema: "islamu_event",
                table: "secret_bindings");

            migrationBuilder.CreateTable(
                name: "configuration_direct_transfer_sessions",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_authority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    target_authority_key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    destination_origin_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    destination_proof_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    nonce_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_byte_length = table.Column<int>(type: "int", nullable: false),
                    next_offset = table.Column<int>(type: "int", nullable: false),
                    last_chunk_offset = table.Column<int>(type: "int", nullable: false),
                    last_chunk_byte_length = table.Column<int>(type: "int", nullable: false),
                    last_chunk_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    source_approved_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    destination_approved_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_direct_transfer_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuration_import_artifacts",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    protected_payload = table.Column<byte[]>(type: "varbinary(max)", maxLength: 4210688, nullable: false),
                    sha256digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    byte_length = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_import_artifacts", x => x.id);
                    table.CheckConstraint("ck_configuration_import_artifacts_byte_length", "byte_length BETWEEN 1 AND 4194304");
                });

            migrationBuilder.CreateTable(
                name: "configuration_import_operations",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    kind = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    target_authority_key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_operation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    artifact_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    target_revision_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    selected_sections_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    mapping_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    approval_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    apply_mode = table.Column<int>(type: "int", nullable: false),
                    snapshot_artifact_handle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    snapshot_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    snapshot_expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    effect_outbox_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    fidelity_verified = table.Column<bool>(type: "bit", nullable: false),
                    fidelity_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    failure_code = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    omitted_section_keys = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    selected_section_keys = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_import_operations", x => x.id);
                    table.CheckConstraint("ck_configuration_import_operations_kind", "kind BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_configuration_import_operations_status", "status BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_configuration_import_operations_target", "((target_authority_key = 'instance' AND target_tenant_id IS NULL) OR (target_authority_key <> 'instance' AND target_tenant_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_configuration_import_operations_configuration_import_operations_source_operation_id",
                        column: x => x.source_operation_id,
                        principalSchema: "islamu_event",
                        principalTable: "configuration_import_operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuration_import_sessions",
                schema: "islamu_event",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    target_scope = table.Column<int>(type: "int", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    target_authority_key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    artifact_handle_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    artifact_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_byte_length = table.Column<int>(type: "int", nullable: false),
                    artifact_expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    access_token_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    state = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    consumed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    preview_artifact_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_target_revision_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_selected_sections_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_mapping_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_required_approval_digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_apply_mode = table.Column<int>(type: "int", nullable: true),
                    preview_expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_import_sessions", x => x.session_id);
                    table.CheckConstraint("ck_configuration_import_sessions_artifact_length", "artifact_byte_length BETWEEN 1 AND 4194304");
                    table.CheckConstraint("ck_configuration_import_sessions_state", "state BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_configuration_import_sessions_target", "((target_scope = 1 AND target_tenant_id IS NULL) OR (target_scope = 2 AND target_tenant_id IS NOT NULL))");
                });

            migrationBuilder.CreateTable(
                name: "configuration_direct_transfer_chunks",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    session_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    offset = table.Column<int>(type: "int", nullable: false),
                    byte_length = table.Column<int>(type: "int", nullable: false),
                    digest = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    protected_payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_direct_transfer_chunks", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuration_direct_transfer_chunks_configuration_direct_transfer_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "islamu_event",
                        principalTable: "configuration_direct_transfer_sessions",
                        principalColumn: "id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                schema: "islamu_event",
                table: "secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL) OR (secret_source_type_id = 1 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_chunks_session_id_offset",
                schema: "islamu_event",
                table: "configuration_direct_transfer_chunks",
                columns: new[] { "session_id", "offset" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_sessions_destination_proof_digest",
                schema: "islamu_event",
                table: "configuration_direct_transfer_sessions",
                column: "destination_proof_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_sessions_nonce_digest",
                schema: "islamu_event",
                table: "configuration_direct_transfer_sessions",
                column: "nonce_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_sessions_target_authority_key_created_at",
                schema: "islamu_event",
                table: "configuration_direct_transfer_sessions",
                columns: new[] { "target_authority_key", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_artifacts_expires_at",
                schema: "islamu_event",
                table: "configuration_import_artifacts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_session_id",
                schema: "islamu_event",
                table: "configuration_import_operations",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_snapshot_artifact_handle_id",
                schema: "islamu_event",
                table: "configuration_import_operations",
                column: "snapshot_artifact_handle_id",
                unique: true,
                filter: "[snapshot_artifact_handle_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_source_operation_id",
                schema: "islamu_event",
                table: "configuration_import_operations",
                column: "source_operation_id");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_target_authority_key_started_at",
                schema: "islamu_event",
                table: "configuration_import_operations",
                columns: new[] { "target_authority_key", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_sessions_artifact_handle_id",
                schema: "islamu_event",
                table: "configuration_import_sessions",
                column: "artifact_handle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_sessions_target_authority_key_state_expires_at",
                schema: "islamu_event",
                table: "configuration_import_sessions",
                columns: new[] { "target_authority_key", "state", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuration_direct_transfer_chunks",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "configuration_import_artifacts",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "configuration_import_operations",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "configuration_import_sessions",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "configuration_direct_transfer_sessions",
                schema: "islamu_event");

            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                schema: "islamu_event",
                table: "secret_bindings");

            migrationBuilder.AddColumn<byte[]>(
                name: "inline_ciphertext",
                schema: "islamu_event",
                table: "secret_bindings",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inline_ciphertext_version",
                schema: "islamu_event",
                table: "secret_bindings",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                schema: "islamu_event",
                table: "secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (secret_source_type_id = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (secret_source_type_id = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
        }
    }
}
