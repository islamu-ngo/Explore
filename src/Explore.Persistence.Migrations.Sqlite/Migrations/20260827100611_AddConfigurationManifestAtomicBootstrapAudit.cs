using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationManifestAtomicBootstrapAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_configuration_manifest_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    mode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    api_version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    manifest_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    instance_section_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    bootstrap_generation = table.Column<int>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    requested_tenant_count = table.Column<int>(type: "INTEGER", nullable: false),
                    created_tenant_count = table.Column<int>(type: "INTEGER", nullable: false),
                    skipped_existing_tenant_count = table.Column<int>(type: "INTEGER", nullable: false),
                    failed_tenant_count = table.Column<int>(type: "INTEGER", nullable: false),
                    reason_code = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    instance_changed_document_key_names = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    instance_changed_setting_key_names = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_manifest_operations", x => x.id);
                    table.CheckConstraint("ck_configuration_manifest_operations_bootstrap_state", "(instance_section_digest IS NULL AND bootstrap_generation IS NULL) OR (instance_section_digest IS NOT NULL AND bootstrap_generation > 0)");
                    table.CheckConstraint("ck_configuration_manifest_operations_counts", "requested_tenant_count >= 0 AND created_tenant_count >= 0 AND skipped_existing_tenant_count >= 0 AND failed_tenant_count >= 0");
                    table.CheckConstraint("ck_configuration_manifest_operations_outcome", "(status = 'Validated' AND mode = 'ValidateOnly' AND created_tenant_count = 0 AND skipped_existing_tenant_count = 0 AND failed_tenant_count = 0 AND reason_code IS NULL AND reason IS NULL) OR (status = 'Applied' AND mode = 'Bootstrap' AND created_tenant_count + skipped_existing_tenant_count = requested_tenant_count AND failed_tenant_count = 0 AND reason_code IS NULL AND reason IS NULL AND instance_section_digest IS NOT NULL AND bootstrap_generation > 0) OR (status = 'Failed' AND created_tenant_count = 0 AND skipped_existing_tenant_count = 0 AND reason_code IS NOT NULL AND reason IS NOT NULL)");
                    table.CheckConstraint("ck_configuration_manifest_operations_timestamps", "completed_at >= started_at");
                });

            migrationBuilder.CreateTable(
                name: "ie_configuration_manifest_tenant_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    reason_code = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    changed_document_key_names = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    changed_setting_key_names = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_manifest_tenant_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_ie_configuration_manifest_tenant_results_ie_configuration_manifest_operations_operation_id",
                        column: x => x.operation_id,
                        principalTable: "ie_configuration_manifest_operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_configuration_manifest_tenant_results_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_manifest_operations_bootstrap_generation_completed",
                table: "ie_configuration_manifest_operations",
                columns: new[] { "status", "bootstrap_generation", "completed_at" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_manifest_operations_digest_mode_completed",
                table: "ie_configuration_manifest_operations",
                columns: new[] { "digest", "mode", "completed_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_manifest_operations_status_completed",
                table: "ie_configuration_manifest_operations",
                columns: new[] { "status", "completed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_manifest_results_tenant_status_completed",
                table: "ie_configuration_manifest_tenant_results",
                columns: new[] { "tenant_id", "status", "completed_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ie_configuration_manifest_tenant_results_operation_id",
                table: "ie_configuration_manifest_tenant_results",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "ux_configuration_manifest_results_tenant_operation",
                table: "ie_configuration_manifest_tenant_results",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_configuration_manifest_tenant_results");

            migrationBuilder.DropTable(
                name: "ie_configuration_manifest_operations");
        }
    }
}
