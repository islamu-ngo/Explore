using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCatalogCommercialDisclosures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "merchant_disclosure_text",
                schema: "islamu_event",
                table: "event_ticket_catalog_versions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_policy_disclosure_text",
                schema: "islamu_event",
                table: "event_ticket_catalog_versions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_contact_disclosure_text",
                schema: "islamu_event",
                table: "event_ticket_catalog_versions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "merchant_disclosure_text",
                schema: "islamu_event",
                table: "event_ticket_catalog_versions");

            migrationBuilder.DropColumn(
                name: "refund_policy_disclosure_text",
                schema: "islamu_event",
                table: "event_ticket_catalog_versions");

            migrationBuilder.DropColumn(
                name: "support_contact_disclosure_text",
                schema: "islamu_event",
                table: "event_ticket_catalog_versions");
        }
    }
}
