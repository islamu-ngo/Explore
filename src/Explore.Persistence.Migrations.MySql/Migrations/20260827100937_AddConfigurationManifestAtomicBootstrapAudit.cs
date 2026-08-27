using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
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
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    mode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    api_version = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    manifest_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    instance_section_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bootstrap_generation = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requested_tenant_count = table.Column<int>(type: "int", nullable: false),
                    created_tenant_count = table.Column<int>(type: "int", nullable: false),
                    skipped_existing_tenant_count = table.Column<int>(type: "int", nullable: false),
                    failed_tenant_count = table.Column<int>(type: "int", nullable: false),
                    reason_code = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    instance_changed_document_key_names = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    instance_changed_setting_key_names = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_manifest_operations", x => x.id);
                    table.CheckConstraint("ck_configuration_manifest_operations_bootstrap_state", "(instance_section_digest IS NULL AND bootstrap_generation IS NULL) OR (instance_section_digest IS NOT NULL AND bootstrap_generation > 0)");
                    table.CheckConstraint("ck_configuration_manifest_operations_counts", "requested_tenant_count >= 0 AND created_tenant_count >= 0 AND skipped_existing_tenant_count >= 0 AND failed_tenant_count >= 0");
                    table.CheckConstraint("ck_configuration_manifest_operations_outcome", "(status = 'Validated' AND mode = 'ValidateOnly' AND created_tenant_count = 0 AND skipped_existing_tenant_count = 0 AND failed_tenant_count = 0 AND reason_code IS NULL AND reason IS NULL) OR (status = 'Applied' AND mode = 'Bootstrap' AND created_tenant_count + skipped_existing_tenant_count = requested_tenant_count AND failed_tenant_count = 0 AND reason_code IS NULL AND reason IS NULL AND instance_section_digest IS NOT NULL AND bootstrap_generation > 0) OR (status = 'Failed' AND created_tenant_count = 0 AND skipped_existing_tenant_count = 0 AND reason_code IS NOT NULL AND reason IS NOT NULL)");
                    table.CheckConstraint("ck_configuration_manifest_operations_timestamps", "completed_at >= started_at");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_configuration_manifest_tenant_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    operation_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason_code = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    changed_document_key_names = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_setting_key_names = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_manifest_tenant_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_ie_configuration_manifest_tenant_results_ie_configur_6FBD2055",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "IX_ie_configuration_manifest_operations_status_bootstra_3773C4A7",
                table: "ie_configuration_manifest_operations",
                columns: new[] { "status", "bootstrap_generation", "completed_at" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_ie_configuration_manifest_tenant_results_operation_id",
                table: "ie_configuration_manifest_tenant_results",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_configuration_manifest_tenant_results_tenant_id_o_7FEDB165",
                table: "ie_configuration_manifest_tenant_results",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_configuration_manifest_tenant_results_tenant_id_s_57D4CF44",
                table: "ie_configuration_manifest_tenant_results",
                columns: new[] { "tenant_id", "status", "completed_at" },
                descending: new[] { false, false, true });
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
