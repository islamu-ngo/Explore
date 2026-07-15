using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceWebhookConfigurationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "configuration_version",
                table: "webhook_endpoints",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "secret_activated_at",
                table: "webhook_endpoints",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "configuration_version",
                table: "webhook_consumers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "webhook_pending_work_decisions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_pending_work_decisions", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_endpoints_configuration_version",
                table: "webhook_endpoints",
                sql: "configuration_version > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_consumers_configuration_version",
                table: "webhook_consumers",
                sql: "configuration_version > 0");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_pending_work_decisions_master_code",
                table: "webhook_pending_work_decisions",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_pending_work_decisions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_endpoints_configuration_version",
                table: "webhook_endpoints");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_consumers_configuration_version",
                table: "webhook_consumers");

            migrationBuilder.DropColumn(
                name: "configuration_version",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "secret_activated_at",
                table: "webhook_endpoints");

            migrationBuilder.DropColumn(
                name: "configuration_version",
                table: "webhook_consumers");
        }
    }
}
