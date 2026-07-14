// ABOUTME: Normalizes incoming webhook processing outcomes and settlement sources into stable lookup tables.
// ABOUTME: Preserves existing enum-valued evidence while adding restrictive relational foreign keys.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeIncomingWebhookEvidenceLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "settlement_source",
                table: "incoming_webhook_messages",
                newName: "settlement_source_id");

            migrationBuilder.AlterColumn<int>(
                name: "settlement_source_id",
                table: "incoming_webhook_messages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.RenameColumn(
                name: "outcome",
                table: "incoming_webhook_processing_attempts",
                newName: "outcome_id");

            migrationBuilder.CreateTable(
                name: "incoming_webhook_processing_attempt_outcomes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_processing_attempt_outcomes", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "incoming_webhook_processing_attempt_outcomes",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "CLAIMED", "Claimed", "A worker acquired a fenced processing lease" },
                    { 2, "PROCESSED", "Processed", "A new business effect and receipt were committed" },
                    { 3, "SETTLED_FROM_RECEIPT", "Settled from receipt", "An existing matching effect receipt proved prior completion" },
                    { 4, "IGNORED", "Ignored", "The verified callback required no business effect" },
                    { 5, "REJECTED_PERMANENT", "Rejected permanently", "The callback could not be processed safely" },
                    { 6, "RETRY_SCHEDULED", "Retry scheduled", "A transient failure scheduled bounded retry work" },
                    { 7, "DEAD_LETTERED", "Dead-lettered", "Automatic processing attempts were exhausted" },
                    { 8, "PAYLOAD_CONFLICT", "Payload conflict", "The provider identity was reused with different exact bytes" },
                    { 9, "LEASE_EXPIRED", "Lease expired", "An unsettled processing lease expired and was recovered" }
                });

            migrationBuilder.CreateTable(
                name: "incoming_webhook_settlement_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incoming_webhook_settlement_sources", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "incoming_webhook_settlement_sources",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "EFFECT_COMMITTED", "Effect committed", "The current execution committed the business effect and receipt" },
                    { 2, "EXISTING_RECEIPT", "Existing receipt", "A matching prior receipt proved the business effect already committed" }
                });

            migrationBuilder.Sql(
                "UPDATE incoming_webhook_messages SET settlement_source_id = NULL WHERE settlement_source_id = 0;");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_processing_attempts_outcome_id",
                table: "incoming_webhook_processing_attempts",
                column: "outcome_id");

            migrationBuilder.CreateIndex(
                name: "ix_incoming_webhook_messages_settlement_source_id",
                table: "incoming_webhook_messages",
                column: "settlement_source_id");

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_processing_attempt_outcomes_master_code",
                table: "incoming_webhook_processing_attempt_outcomes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_incoming_webhook_settlement_sources_master_code",
                table: "incoming_webhook_settlement_sources",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_messages_incoming_webhook_settlement_sourc",
                table: "incoming_webhook_messages",
                column: "settlement_source_id",
                principalTable: "incoming_webhook_settlement_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_incoming_webhook_processing_attempts_incoming_webhook_proce",
                table: "incoming_webhook_processing_attempts",
                column: "outcome_id",
                principalTable: "incoming_webhook_processing_attempt_outcomes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_messages_incoming_webhook_settlement_sourc",
                table: "incoming_webhook_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_incoming_webhook_processing_attempts_incoming_webhook_proce",
                table: "incoming_webhook_processing_attempts");

            migrationBuilder.DropTable(
                name: "incoming_webhook_processing_attempt_outcomes");

            migrationBuilder.DropTable(
                name: "incoming_webhook_settlement_sources");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_processing_attempts_outcome_id",
                table: "incoming_webhook_processing_attempts");

            migrationBuilder.DropIndex(
                name: "ix_incoming_webhook_messages_settlement_source_id",
                table: "incoming_webhook_messages");

            migrationBuilder.Sql(
                "UPDATE incoming_webhook_messages SET settlement_source_id = 0 WHERE settlement_source_id IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "settlement_source_id",
                table: "incoming_webhook_messages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "settlement_source_id",
                table: "incoming_webhook_messages",
                newName: "settlement_source");

            migrationBuilder.RenameColumn(
                name: "outcome_id",
                table: "incoming_webhook_processing_attempts",
                newName: "outcome");
        }
    }
}
