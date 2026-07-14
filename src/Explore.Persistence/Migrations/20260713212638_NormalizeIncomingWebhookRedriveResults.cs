// ABOUTME: Normalizes incoming webhook redrive results into a stable relational lookup table.
// ABOUTME: Preserves existing scheduled evidence while adding a restrictive result foreign key.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeIncomingWebhookRedriveResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "result",
                table: "incoming_webhook_redrive_records",
                newName: "result_id");

            migrationBuilder.CreateTable(
                name: "incoming_webhook_redrive_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_redrive_results", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "incoming_webhook_redrive_results",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[]
                {
                    1,
                    "SCHEDULED",
                    "Scheduled",
                    "An authorized operator created a new processing generation"
                });

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_redrive_records_result_id",
                table: "incoming_webhook_redrive_records",
                column: "result_id");

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_redrive_results_master_code",
                table: "incoming_webhook_redrive_results",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_redrive_records_incoming_webhook_redrive_r",
                table: "incoming_webhook_redrive_records",
                column: "result_id",
                principalTable: "incoming_webhook_redrive_results",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_redrive_records_incoming_webhook_redrive_r",
                table: "incoming_webhook_redrive_records");

            migrationBuilder.DropTable(
                name: "incoming_webhook_redrive_results");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_redrive_records_result_id",
                table: "incoming_webhook_redrive_records");

            migrationBuilder.RenameColumn(
                name: "result_id",
                table: "incoming_webhook_redrive_records",
                newName: "result");
        }
    }
}
