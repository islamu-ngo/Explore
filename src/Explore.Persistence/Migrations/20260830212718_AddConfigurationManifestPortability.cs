using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_authority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_authority_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_origin_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    destination_proof_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    nonce_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_byte_length = table.Column<int>(type: "integer", nullable: false),
                    next_offset = table.Column<int>(type: "integer", nullable: false),
                    last_chunk_offset = table.Column<int>(type: "integer", nullable: false),
                    last_chunk_byte_length = table.Column<int>(type: "integer", nullable: false),
                    last_chunk_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    source_approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    destination_approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_payload = table.Column<byte[]>(type: "bytea", maxLength: 4210688, nullable: false),
                    sha256digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    byte_length = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    target_authority_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_operation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    artifact_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    target_revision_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    selected_sections_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    mapping_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    approval_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    apply_mode = table.Column<int>(type: "integer", nullable: false),
                    snapshot_artifact_handle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    snapshot_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    snapshot_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effect_outbox_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fidelity_verified = table.Column<bool>(type: "boolean", nullable: false),
                    fidelity_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    failure_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    omitted_section_keys = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    selected_section_keys = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_import_operations", x => x.id);
                    table.CheckConstraint("ck_configuration_import_operations_kind", "kind BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_configuration_import_operations_status", "status BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_configuration_import_operations_target", "((target_authority_key = 'instance' AND target_tenant_id IS NULL) OR (target_authority_key <> 'instance' AND target_tenant_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_configuration_import_operations_configuration_i_9cc41bd28a7f",
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
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_scope = table.Column<int>(type: "integer", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_authority_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    artifact_handle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artifact_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    artifact_byte_length = table.Column<int>(type: "integer", nullable: false),
                    artifact_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    access_token_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    preview_artifact_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_target_revision_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_selected_sections_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_mapping_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_required_approval_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    preview_apply_mode = table.Column<int>(type: "integer", nullable: true),
                    preview_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offset = table.Column<int>(type: "integer", nullable: false),
                    byte_length = table.Column<int>(type: "integer", nullable: false),
                    digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    protected_payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_direct_transfer_chunks", x => x.id);
                    table.ForeignKey(
                        name: "fk_configuration_direct_transfer_chunks_configurat_278d92b11536",
                        column: x => x.session_id,
                        principalSchema: "islamu_event",
                        principalTable: "configuration_direct_transfer_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "ix_configuration_direct_transfer_sessions_destinat_ac6747680381",
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
                name: "ix_configuration_direct_transfer_sessions_target_a_adae0daf40d8",
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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_source_operation_id",
                schema: "islamu_event",
                table: "configuration_import_operations",
                column: "source_operation_id");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_operations_target_authorit_df72b18e4d91",
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
                name: "ix_configuration_import_sessions_target_authority__846f29f3bbe1",
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
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inline_ciphertext_version",
                schema: "islamu_event",
                table: "secret_bindings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                schema: "islamu_event",
                table: "secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (secret_source_type_id = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (secret_source_type_id = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
        }
    }
}
