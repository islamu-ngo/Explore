using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalImportedRegistrationFormVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_schema_revisions_tenant_id__87717C8A",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.AddColumn<int>(
                name: "drift_class_id",
                table: "ie_registration_provider_schema_revisions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "provider_snapshot_json",
                table: "ie_registration_provider_schema_revisions",
                type: "text",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_snapshot_sha256hash",
                table: "ie_registration_provider_schema_revisions",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_id",
                table: "ie_registration_provider_schema_revisions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_revision_id",
                table: "ie_registration_provider_schema_revisions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "adapter_policy_version",
                table: "ie_registration_provider_connections",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "api_version",
                table: "ie_registration_provider_connections",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "conformance_evidence_revision",
                table: "ie_registration_provider_connections",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "management_api_base_url",
                table: "ie_registration_provider_connections",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_code",
                table: "ie_registration_provider_connections",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_deployment_code",
                table: "ie_registration_provider_connections",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_workspace_id",
                table: "ie_registration_provider_connections",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "public_base_url",
                table: "ie_registration_provider_connections",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_id",
                table: "ie_registration_provider_bindings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_revision_id",
                table: "ie_registration_provider_bindings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider_webhook_id",
                table: "ie_registration_provider_bindings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "webhook_secret_binding_id",
                table: "ie_registration_provider_bindings",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "external_import_mapping_revision_hash",
                table: "ie_registration_form_versions",
                type: "varchar(44)",
                maxLength: 44,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "external_provider_survey_id",
                table: "ie_registration_form_versions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "external_provider_survey_revision_id",
                table: "ie_registration_form_versions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "external_registration_provider_connection_id",
                table: "ie_registration_form_versions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "external_registration_provider_schema_revision_id",
                table: "ie_registration_form_versions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "source_kind_id",
                table: "ie_registration_form_versions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_provider_schema_revisions_tenant_id_id",
                table: "ie_registration_provider_schema_revisions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "ie_registration_form_version_source_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_form_version_source_kinds", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_provider_schema_revisions_drift_class_id",
                table: "ie_registration_provider_schema_revisions",
                column: "drift_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_schema_revisions_tenant_id__2E61ADF5",
                table: "ie_registration_provider_schema_revisions",
                columns: new[] { "tenant_id", "registration_provider_connection_id", "provider_survey_id", "revision_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_connections_tenant_id_provi_3F56F9F2",
                table: "ie_registration_provider_connections",
                columns: new[] { "tenant_id", "provider_code", "provider_deployment_code", "api_version", "adapter_policy_version", "conformance_evidence_revision", "provider_workspace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_bindings_tenant_id_webhook__77525641",
                table: "ie_registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_form_versions_external_registration__77CC7B2D",
                table: "ie_registration_form_versions",
                column: "external_registration_provider_schema_revision_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_form_versions_external_registration__84C0A657",
                table: "ie_registration_form_versions",
                column: "external_registration_provider_connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_form_versions_source_kind_id",
                table: "ie_registration_form_versions",
                column: "source_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_registration_form_version_source_kinds_master_code",
                table: "ie_registration_form_version_source_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_form_versions_ie_registration_form_v_7E648D0B",
                table: "ie_registration_form_versions",
                column: "source_kind_id",
                principalTable: "ie_registration_form_version_source_kinds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_form_versions_ie_registration_provid_1F791302",
                table: "ie_registration_form_versions",
                column: "external_registration_provider_connection_id",
                principalTable: "ie_registration_provider_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_form_versions_ie_registration_provid_3BF4EB0C",
                table: "ie_registration_form_versions",
                column: "external_registration_provider_schema_revision_id",
                principalTable: "ie_registration_provider_schema_revisions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_bindings_ie_secret_bindings_8A893DE3",
                table: "ie_registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalTable: "ie_secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_registration_provider_schema_revisions_ie_registr_A02373BB",
                table: "ie_registration_provider_schema_revisions",
                column: "drift_class_id",
                principalTable: "ie_registration_provider_drift_classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_form_versions_ie_registration_form_v_7E648D0B",
                table: "ie_registration_form_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_form_versions_ie_registration_provid_1F791302",
                table: "ie_registration_form_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_form_versions_ie_registration_provid_3BF4EB0C",
                table: "ie_registration_form_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_bindings_ie_secret_bindings_8A893DE3",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "FK_ie_registration_provider_schema_revisions_ie_registr_A02373BB",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropTable(
                name: "ie_registration_form_version_source_kinds");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_provider_schema_revisions_tenant_id_id",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_provider_schema_revisions_drift_class_id",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_schema_revisions_tenant_id__2E61ADF5",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_connections_tenant_id_provi_3F56F9F2",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_provider_bindings_tenant_id_webhook__77525641",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_form_versions_external_registration__77CC7B2D",
                table: "ie_registration_form_versions");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_form_versions_external_registration__84C0A657",
                table: "ie_registration_form_versions");

            migrationBuilder.DropIndex(
                name: "ix_ie_registration_form_versions_source_kind_id",
                table: "ie_registration_form_versions");

            migrationBuilder.DropColumn(
                name: "drift_class_id",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_snapshot_json",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_snapshot_sha256hash",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_survey_id",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_survey_revision_id",
                table: "ie_registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "adapter_policy_version",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "api_version",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "conformance_evidence_revision",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "management_api_base_url",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_code",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_deployment_code",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_workspace_id",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "public_base_url",
                table: "ie_registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_survey_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "provider_survey_revision_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "provider_webhook_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "webhook_secret_binding_id",
                table: "ie_registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "external_import_mapping_revision_hash",
                table: "ie_registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_provider_survey_id",
                table: "ie_registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_provider_survey_revision_id",
                table: "ie_registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_registration_provider_connection_id",
                table: "ie_registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_registration_provider_schema_revision_id",
                table: "ie_registration_form_versions");

            migrationBuilder.DropColumn(
                name: "source_kind_id",
                table: "ie_registration_form_versions");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_schema_revisions_tenant_id__87717C8A",
                table: "ie_registration_provider_schema_revisions",
                columns: new[] { "tenant_id", "registration_provider_connection_id", "revision_hash" },
                unique: true);
        }
    }
}
