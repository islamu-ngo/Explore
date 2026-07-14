using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeWebhookProviderCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_provider_capabilities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_capabilities", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_consumer_provider_bindings_capabilities_known",
                table: "webhook_consumer_provider_bindings",
                sql: "capabilities >= 0 AND capabilities <= 4095");

            migrationBuilder.AddCheckConstraint(
                name: "ck_webhook_consumer_provider_bindings_governance_capabilities_~",
                table: "webhook_consumer_provider_bindings",
                sql: "governance_allowed_capabilities >= 0 AND governance_allowed_capabilities <= 4095");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_capabilities_master_code",
                table: "webhook_provider_capabilities",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_provider_capabilities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_consumer_provider_bindings_capabilities_known",
                table: "webhook_consumer_provider_bindings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_webhook_consumer_provider_bindings_governance_capabilities_~",
                table: "webhook_consumer_provider_bindings");
        }
    }
}
