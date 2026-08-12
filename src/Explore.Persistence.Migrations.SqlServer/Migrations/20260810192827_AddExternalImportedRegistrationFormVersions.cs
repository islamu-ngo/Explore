using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalImportedRegistrationFormVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_registration_provider_schema_revisions_tenant_id_registration_provider_connection_id_revision_hash",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.AddColumn<int>(
                name: "drift_class_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "provider_snapshot_json",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_snapshot_sha256hash",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_revision_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "adapter_policy_version",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "api_version",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "conformance_evidence_revision",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "management_api_base_url",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_code",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_deployment_code",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_workspace_id",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "public_base_url",
                schema: "islamu_event",
                table: "registration_provider_connections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_id",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_survey_revision_id",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_webhook_id",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_import_mapping_revision_hash",
                schema: "islamu_event",
                table: "registration_form_versions",
                type: "nvarchar(44)",
                maxLength: 44,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_provider_survey_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_provider_survey_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_registration_provider_connection_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "external_registration_provider_schema_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_kind_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_provider_schema_revisions_tenant_id_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "registration_form_version_source_kinds",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_form_version_source_kinds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_schema_revisions_drift_class_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                column: "drift_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_schema_revisions_tenant_id_registration_provider_connection_id_provider_survey_id_revision_hash",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                columns: new[] { "tenant_id", "registration_provider_connection_id", "provider_survey_id", "revision_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_connections_tenant_id_provider_code_provider_deployment_code_api_version_adapter_policy_version_confor",
                schema: "islamu_event",
                table: "registration_provider_connections",
                columns: new[] { "tenant_id", "provider_code", "provider_deployment_code", "api_version", "adapter_policy_version", "conformance_evidence_revision", "provider_workspace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_bindings_tenant_id_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_form_versions_external_registration_provider_connection_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                column: "external_registration_provider_connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_form_versions_external_registration_provider_schema_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                column: "external_registration_provider_schema_revision_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_form_versions_source_kind_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                column: "source_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_form_version_source_kinds_master_code",
                schema: "islamu_event",
                table: "registration_form_version_source_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_form_versions_registration_form_version_source_kinds_source_kind_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                column: "source_kind_id",
                principalSchema: "islamu_event",
                principalTable: "registration_form_version_source_kinds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_form_versions_registration_provider_connections_external_registration_provider_connection_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                column: "external_registration_provider_connection_id",
                principalSchema: "islamu_event",
                principalTable: "registration_provider_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_form_versions_registration_provider_schema_revisions_external_registration_provider_schema_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions",
                column: "external_registration_provider_schema_revision_id",
                principalSchema: "islamu_event",
                principalTable: "registration_provider_schema_revisions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_bindings_secret_bindings_tenant_id_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings",
                columns: new[] { "tenant_id", "webhook_secret_binding_id" },
                principalSchema: "islamu_event",
                principalTable: "secret_bindings",
                principalColumns: new[] { "scope_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_provider_schema_revisions_registration_provider_drift_classes_drift_class_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                column: "drift_class_id",
                principalSchema: "islamu_event",
                principalTable: "registration_provider_drift_classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_registration_form_versions_registration_form_version_source_kinds_source_kind_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_form_versions_registration_provider_connections_external_registration_provider_connection_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_form_versions_registration_provider_schema_revisions_external_registration_provider_schema_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_bindings_secret_bindings_tenant_id_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_provider_schema_revisions_registration_provider_drift_classes_drift_class_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropTable(
                name: "registration_form_version_source_kinds",
                schema: "islamu_event");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_provider_schema_revisions_tenant_id_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_schema_revisions_drift_class_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_schema_revisions_tenant_id_registration_provider_connection_id_provider_survey_id_revision_hash",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_connections_tenant_id_provider_code_provider_deployment_code_api_version_adapter_policy_version_confor",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropIndex(
                name: "ix_registration_provider_bindings_tenant_id_webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropIndex(
                name: "ix_registration_form_versions_external_registration_provider_connection_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropIndex(
                name: "ix_registration_form_versions_external_registration_provider_schema_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropIndex(
                name: "ix_registration_form_versions_source_kind_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropColumn(
                name: "drift_class_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_snapshot_json",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_snapshot_sha256hash",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_survey_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "provider_survey_revision_id",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions");

            migrationBuilder.DropColumn(
                name: "adapter_policy_version",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "api_version",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "conformance_evidence_revision",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "management_api_base_url",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_code",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_deployment_code",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_workspace_id",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "public_base_url",
                schema: "islamu_event",
                table: "registration_provider_connections");

            migrationBuilder.DropColumn(
                name: "provider_survey_id",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "provider_survey_revision_id",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "provider_webhook_id",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "webhook_secret_binding_id",
                schema: "islamu_event",
                table: "registration_provider_bindings");

            migrationBuilder.DropColumn(
                name: "external_import_mapping_revision_hash",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_provider_survey_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_provider_survey_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_registration_provider_connection_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropColumn(
                name: "external_registration_provider_schema_revision_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.DropColumn(
                name: "source_kind_id",
                schema: "islamu_event",
                table: "registration_form_versions");

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_schema_revisions_tenant_id_registration_provider_connection_id_revision_hash",
                schema: "islamu_event",
                table: "registration_provider_schema_revisions",
                columns: new[] { "tenant_id", "registration_provider_connection_id", "revision_hash" },
                unique: true);
        }
    }
}
