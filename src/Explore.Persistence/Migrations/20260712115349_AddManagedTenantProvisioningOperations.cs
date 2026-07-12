// ABOUTME: Adds durable managed tenant provisioning operations and global host uniqueness.
// ABOUTME: Guards existing domain data before enforcing permanent request and customer invariants.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedTenantProvisioningOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    conflicting_host text;
                BEGIN
                    SELECT rtrim(lower(btrim(value::jsonb #>> '{}')), '.')
                    INTO conflicting_host
                    FROM tenant_setting_overrides
                    WHERE setting_key IN ('domains.tenant_subdomain', 'domains.tenant_custom_domain')
                    GROUP BY rtrim(lower(btrim(value::jsonb #>> '{}')), '.')
                    HAVING count(*) > 1
                    LIMIT 1;

                    IF conflicting_host IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot enforce tenant domain host uniqueness: duplicate normalized host % exists.', conflicting_host
                            USING HINT = 'Resolve the conflicting tenant domain settings explicitly; this migration never deletes tenant data.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_tenant_setting_domain_value;");
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_tenant_setting_domain_host_normalized
                ON tenant_setting_overrides ((rtrim(lower(btrim(value::jsonb #>> '{}')), '.')))
                WHERE setting_key IN ('domains.tenant_subdomain', 'domains.tenant_custom_domain');
                """);

            migrationBuilder.CreateTable(
                name: "managed_tenant_provisioning_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    managed_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_customer_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    request_json = table.Column<string>(type: "jsonb", nullable: true),
                    tenant_slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_administrator_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_managed_tenant_provisioning_operations", x => x.id);
                    table.CheckConstraint("ck_managed_tenant_provisioning_cancelled", "(status = 'Cancelled') = (cancelled_at IS NOT NULL)");
                    table.CheckConstraint("ck_managed_tenant_provisioning_failed", "(status = 'Failed') = (failure_code IS NOT NULL AND failed_at IS NOT NULL)");
                    table.CheckConstraint("ck_managed_tenant_provisioning_request_snapshot", "(status IN ('Pending', 'Processing')) = (request_json IS NOT NULL)");
                    table.CheckConstraint("ck_managed_tenant_provisioning_terminal_result", "(status = 'Succeeded') = (tenant_id IS NOT NULL AND tenant_administrator_user_id IS NOT NULL AND completed_at IS NOT NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_managed_tenant_provisioning_operations_status_created_at",
                table: "managed_tenant_provisioning_operations",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_managed_tenant_provisioning_operations_tenant_id",
                table: "managed_tenant_provisioning_operations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_managed_tenant_provisioning_instance_customer",
                table: "managed_tenant_provisioning_operations",
                columns: new[] { "managed_instance_id", "external_customer_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_managed_tenant_provisioning_instance_request",
                table: "managed_tenant_provisioning_operations",
                columns: new[] { "managed_instance_id", "external_request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_tenant_setting_domain_host_normalized;");

            migrationBuilder.DropTable(
                name: "managed_tenant_provisioning_operations");
        }
    }
}
