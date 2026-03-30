using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantNavigationLinkAuditingAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                table: "external_api_keys",
                newName: "external_api_key_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_external_api_keys_tenant_id_status",
                table: "external_api_keys",
                newName: "ix_external_api_keys_tenant_id_external_api_key_status_id");

            // Step 1: Add column with temporary server default to backfill existing rows
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "tenant_navigation_links",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            // Step 2: Remove the server default — app code (SaveChangesAsync interceptor) sets CreatedAt
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "tenant_navigation_links",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "tenant_navigation_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tenant_navigation_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "tenant_navigation_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tenant_navigation_links",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "tenant_navigation_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "tenant_navigation_links",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "credit_limit",
                table: "external_api_keys",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "external_api_keys",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "external_api_key_credit_period_id",
                table: "external_api_keys",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "max_rollover_credits",
                table: "external_api_keys",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "external_api_key_credit_periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_credit_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_api_key_quotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_api_key_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    credit_limit = table.Column<int>(type: "integer", nullable: false),
                    credits_used = table.Column<int>(type: "integer", nullable: false),
                    rollover_credits = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_quotas", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_api_key_quotas_external_api_keys_external_api_key_",
                        column: x => x.external_api_key_id,
                        principalTable: "external_api_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_api_key_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_usable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_api_key_statuses", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_external_api_key_credit_period_id",
                table: "external_api_keys",
                column: "external_api_key_credit_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_keys_external_api_key_status_id",
                table: "external_api_keys",
                column: "external_api_key_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_api_key_quotas_external_api_key_id_period_start",
                table: "external_api_key_quotas",
                columns: new[] { "external_api_key_id", "period_start" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_external_api_keys_external_api_key_credit_periods_external_",
                table: "external_api_keys",
                column: "external_api_key_credit_period_id",
                principalTable: "external_api_key_credit_periods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_external_api_keys_external_api_key_statuses_external_api_ke",
                table: "external_api_keys",
                column: "external_api_key_status_id",
                principalTable: "external_api_key_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_external_api_keys_external_api_key_credit_periods_external_",
                table: "external_api_keys");

            migrationBuilder.DropForeignKey(
                name: "fk_external_api_keys_external_api_key_statuses_external_api_ke",
                table: "external_api_keys");

            migrationBuilder.DropTable(
                name: "external_api_key_credit_periods");

            migrationBuilder.DropTable(
                name: "external_api_key_quotas");

            migrationBuilder.DropTable(
                name: "external_api_key_statuses");

            migrationBuilder.DropIndex(
                name: "ix_external_api_keys_external_api_key_credit_period_id",
                table: "external_api_keys");

            migrationBuilder.DropIndex(
                name: "ix_external_api_keys_external_api_key_status_id",
                table: "external_api_keys");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "tenant_navigation_links");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "tenant_navigation_links");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tenant_navigation_links");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "tenant_navigation_links");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tenant_navigation_links");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "tenant_navigation_links");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "tenant_navigation_links");

            migrationBuilder.DropColumn(
                name: "credit_limit",
                table: "external_api_keys");

            migrationBuilder.DropColumn(
                name: "description",
                table: "external_api_keys");

            migrationBuilder.DropColumn(
                name: "external_api_key_credit_period_id",
                table: "external_api_keys");

            migrationBuilder.DropColumn(
                name: "max_rollover_credits",
                table: "external_api_keys");

            migrationBuilder.RenameColumn(
                name: "external_api_key_status_id",
                table: "external_api_keys",
                newName: "status");

            migrationBuilder.RenameIndex(
                name: "ix_external_api_keys_tenant_id_external_api_key_status_id",
                table: "external_api_keys",
                newName: "ix_external_api_keys_tenant_id_status");
        }
    }
}
