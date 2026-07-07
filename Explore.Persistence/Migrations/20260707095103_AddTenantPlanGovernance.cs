using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPlanGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_plan_application_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_successful = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_application_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_assignment_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active_assignment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_assignment_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    allows_provisioning = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    tenant_plan_status_id = table.Column<int>(type: "integer", nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    billing_period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active_for_provisioning = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_plan_versions_tenant_plan_statuses_tenant_plan_statu",
                        column: x => x.tenant_plan_status_id,
                        principalTable: "tenant_plan_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_versions_tenant_plans_tenant_plan_id",
                        column: x => x.tenant_plan_id,
                        principalTable: "tenant_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_assignment_status_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_plan_assignments_tenant_plan_assignment_statuses_ten",
                        column: x => x.tenant_plan_assignment_status_id,
                        principalTable: "tenant_plan_assignment_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_assignments_tenant_plan_versions_tenant_plan_ve",
                        column: x => x.tenant_plan_version_id,
                        principalTable: "tenant_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_assignments_tenant_plans_tenant_plan_id",
                        column: x => x.tenant_plan_id,
                        principalTable: "tenant_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_version_quotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quota_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    limit = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_version_quotas", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_plan_version_quotas_tenant_plan_versions_tenant_plan",
                        column: x => x.tenant_plan_version_id,
                        principalTable: "tenant_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_version_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setting_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    json_value = table.Column<string>(type: "jsonb", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_version_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_plan_version_settings_tenant_plan_versions_tenant_pl",
                        column: x => x.tenant_plan_version_id,
                        principalTable: "tenant_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_plan_application_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_plan_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_plan_application_status_id = table.Column<int>(type: "integer", nullable: false),
                    applied_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    previous_tenant_plan_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_setting_keys_json = table.Column<string>(type: "jsonb", nullable: false),
                    changed_quota_keys_json = table.Column<string>(type: "jsonb", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_plan_application_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_plan_application_logs_tenant_plan_application_status",
                        column: x => x.tenant_plan_application_status_id,
                        principalTable: "tenant_plan_application_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_application_logs_tenant_plan_assignments_tenant",
                        column: x => x.tenant_plan_assignment_id,
                        principalTable: "tenant_plan_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_application_logs_tenant_plan_versions_previous_",
                        column: x => x.previous_tenant_plan_version_id,
                        principalTable: "tenant_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_application_logs_tenant_plan_versions_tenant_pl",
                        column: x => x.tenant_plan_version_id,
                        principalTable: "tenant_plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_application_logs_tenant_plans_tenant_plan_id",
                        column: x => x.tenant_plan_id,
                        principalTable: "tenant_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_plan_application_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_logs_applied_by_user_id",
                table: "tenant_plan_application_logs",
                column: "applied_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_logs_previous_tenant_plan_version_id",
                table: "tenant_plan_application_logs",
                column: "previous_tenant_plan_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_logs_tenant_id_applied_at",
                table: "tenant_plan_application_logs",
                columns: new[] { "tenant_id", "applied_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_logs_tenant_plan_application_status",
                table: "tenant_plan_application_logs",
                column: "tenant_plan_application_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_logs_tenant_plan_assignment_id",
                table: "tenant_plan_application_logs",
                column: "tenant_plan_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_logs_tenant_plan_id",
                table: "tenant_plan_application_logs",
                column: "tenant_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_logs_tenant_plan_version_id",
                table: "tenant_plan_application_logs",
                column: "tenant_plan_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_application_statuses_master_code",
                table: "tenant_plan_application_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_assignment_statuses_master_code",
                table: "tenant_plan_assignment_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_assignments_tenant_plan_assignment_status_id",
                table: "tenant_plan_assignments",
                column: "tenant_plan_assignment_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_assignments_tenant_plan_id",
                table: "tenant_plan_assignments",
                column: "tenant_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_assignments_tenant_plan_version_id_tenant_plan_",
                table: "tenant_plan_assignments",
                columns: new[] { "tenant_plan_version_id", "tenant_plan_assignment_status_id" });

            migrationBuilder.CreateIndex(
                name: "ux_tenant_plan_assignments_active_tenant",
                table: "tenant_plan_assignments",
                column: "tenant_id",
                unique: true,
                filter: "tenant_plan_assignment_status_id = 1");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_statuses_master_code",
                table: "tenant_plan_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_version_quotas_tenant_plan_version_id_quota_key",
                table: "tenant_plan_version_quotas",
                columns: new[] { "tenant_plan_version_id", "quota_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_version_settings_tenant_plan_version_id_setting",
                table: "tenant_plan_version_settings",
                columns: new[] { "tenant_plan_version_id", "setting_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_versions_tenant_plan_id_version_number",
                table: "tenant_plan_versions",
                columns: new[] { "tenant_plan_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plan_versions_tenant_plan_status_id_is_active_for_pr",
                table: "tenant_plan_versions",
                columns: new[] { "tenant_plan_status_id", "is_active_for_provisioning" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_plans_key",
                table: "tenant_plans",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_plan_application_logs");

            migrationBuilder.DropTable(
                name: "tenant_plan_version_quotas");

            migrationBuilder.DropTable(
                name: "tenant_plan_version_settings");

            migrationBuilder.DropTable(
                name: "tenant_plan_application_statuses");

            migrationBuilder.DropTable(
                name: "tenant_plan_assignments");

            migrationBuilder.DropTable(
                name: "tenant_plan_assignment_statuses");

            migrationBuilder.DropTable(
                name: "tenant_plan_versions");

            migrationBuilder.DropTable(
                name: "tenant_plan_statuses");

            migrationBuilder.DropTable(
                name: "tenant_plans");
        }
    }
}
