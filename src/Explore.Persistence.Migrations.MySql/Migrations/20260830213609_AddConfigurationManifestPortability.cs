using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationManifestPortability : Migration
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

            migrationBuilder.CreateTable(
                name: "ie_configuration_direct_transfer_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    source_authority = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_authority_key = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    destination_origin_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    destination_proof_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nonce_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artifact_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artifact_byte_length = table.Column<int>(type: "int", nullable: false),
                    next_offset = table.Column<int>(type: "int", nullable: false),
                    last_chunk_offset = table.Column<int>(type: "int", nullable: false),
                    last_chunk_byte_length = table.Column<int>(type: "int", nullable: false),
                    last_chunk_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_approved_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    destination_approved_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_direct_transfer_sessions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_configuration_import_artifacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    protected_payload = table.Column<byte[]>(type: "longblob", maxLength: 4210688, nullable: false),
                    sha256digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    byte_length = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_import_artifacts", x => x.id);
                    table.CheckConstraint("ck_configuration_import_artifacts_byte_length", "byte_length BETWEEN 1 AND 4194304");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_configuration_import_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    session_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    kind = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    target_authority_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    actor_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    source_operation_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    artifact_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_revision_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    selected_sections_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mapping_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approval_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    apply_mode = table.Column<int>(type: "int", nullable: false),
                    snapshot_artifact_handle_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    snapshot_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    snapshot_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    effect_outbox_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    fidelity_verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    fidelity_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    failure_code = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    failure_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    omitted_section_keys = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    selected_section_keys = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_import_operations", x => x.id);
                    table.CheckConstraint("ck_configuration_import_operations_kind", "kind BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_configuration_import_operations_status", "status BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_configuration_import_operations_target", "((target_authority_key = 'instance' AND target_tenant_id IS NULL) OR (target_authority_key <> 'instance' AND target_tenant_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_ie_configuration_import_operations_ie_configuration__2c06e992",
                        column: x => x.source_operation_id,
                        principalTable: "ie_configuration_import_operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_configuration_import_sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    target_scope = table.Column<int>(type: "int", nullable: false),
                    target_tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    target_authority_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artifact_handle_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    artifact_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artifact_byte_length = table.Column<int>(type: "int", nullable: false),
                    artifact_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    access_token_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    state = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    consumed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    preview_artifact_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preview_target_revision_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preview_selected_sections_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preview_mapping_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preview_required_approval_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preview_apply_mode = table.Column<int>(type: "int", nullable: true),
                    preview_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_import_sessions", x => x.session_id);
                    table.CheckConstraint("ck_configuration_import_sessions_artifact_length", "artifact_byte_length BETWEEN 1 AND 4194304");
                    table.CheckConstraint("ck_configuration_import_sessions_state", "state BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_configuration_import_sessions_target", "((target_scope = 1 AND target_tenant_id IS NULL) OR (target_scope = 2 AND target_tenant_id IS NOT NULL))");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_configuration_direct_transfer_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    session_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    offset = table.Column<int>(type: "int", nullable: false),
                    byte_length = table.Column<int>(type: "int", nullable: false),
                    digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    protected_payload = table.Column<byte[]>(type: "longblob", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_direct_transfer_chunks", x => x.id);
                    table.ForeignKey(
                        name: "fk_ie_configuration_direct_transfer_chunks_ie_configura_c2d603d8",
                        column: x => x.session_id,
                        principalTable: "ie_configuration_direct_transfer_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL) OR (secret_source_type_id = 1 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_chunks_session_id_offset",
                table: "ie_configuration_direct_transfer_chunks",
                columns: new[] { "session_id", "offset" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuration_direct_transfer_sessions_nonce_digest",
                table: "ie_configuration_direct_transfer_sessions",
                column: "nonce_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_configuration_direct_transfer_sessions_destinatio_385d5ea1",
                table: "ie_configuration_direct_transfer_sessions",
                column: "destination_proof_digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_configuration_direct_transfer_sessions_target_aut_b38a2122",
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
                name: "ix_configuration_import_operations_source_operation_id",
                table: "ie_configuration_import_operations",
                column: "source_operation_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_configuration_import_operations_snapshot_artifact_446cadf8",
                table: "ie_configuration_import_operations",
                column: "snapshot_artifact_handle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_configuration_import_operations_target_authority__3173b34b",
                table: "ie_configuration_import_operations",
                columns: new[] { "target_authority_key", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_import_sessions_artifact_handle_id",
                table: "ie_configuration_import_sessions",
                column: "artifact_handle_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_configuration_import_sessions_target_authority_ke_d70ed059",
                table: "ie_configuration_import_sessions",
                columns: new[] { "target_authority_key", "state", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_configuration_direct_transfer_chunks");

            migrationBuilder.DropTable(
                name: "ie_configuration_import_artifacts");

            migrationBuilder.DropTable(
                name: "ie_configuration_import_operations");

            migrationBuilder.DropTable(
                name: "ie_configuration_import_sessions");

            migrationBuilder.DropTable(
                name: "ie_configuration_direct_transfer_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings");

            migrationBuilder.AddColumn<byte[]>(
                name: "inline_ciphertext",
                table: "ie_secret_bindings",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inline_ciphertext_version",
                table: "ie_secret_bindings",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_secret_bindings_source_metadata",
                table: "ie_secret_bindings",
                sql: "(secret_source_type_id = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL   AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) OR (secret_source_type_id = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) OR (secret_source_type_id = 2 AND environment_variable_name IS NOT NULL   AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
        }
    }
}
