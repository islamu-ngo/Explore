// ABOUTME: Normalizes provider publication attempt outcomes into a stable relational lookup.
// ABOUTME: Preserves existing evidence IDs and adds restrictive outcome integrity plus query indexing.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeWebhookProviderPublicationAttemptOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_webhook_provider_publication_attempts_webhook_provider_publ",
                table: "webhook_provider_publication_attempts");

            migrationBuilder.RenameColumn(
                name: "outcome",
                table: "webhook_provider_publication_attempts",
                newName: "outcome_id");

            migrationBuilder.CreateTable(
                name: "webhook_provider_publication_attempt_outcomes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_provider_publication_attempt_outcomes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publication_attempts_outcome_id",
                table: "webhook_provider_publication_attempts",
                column: "outcome_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_provider_publication_attempts_tenant_outcome_recorded",
                table: "webhook_provider_publication_attempts",
                columns: new[] { "tenant_id", "outcome_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_provider_publication_attempt_outcomes_master_code",
                table: "webhook_provider_publication_attempt_outcomes",
                column: "master_code",
                unique: true);

            migrationBuilder.InsertData(
                table: "webhook_provider_publication_attempt_outcomes",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "PUBLISHING_STARTED", "Publishing started", "A worker acquired a fenced provider submission claim" },
                    { 2, "PROVIDER_QUEUED", "Provider queued", "The provider accepted the publication" },
                    { 3, "RETRY_SCHEDULED", "Retry scheduled", "A definitely-not-accepted submission scheduled bounded retry" },
                    { 4, "PUBLICATION_UNKNOWN", "Publication unknown", "Submission acceptance could not be determined safely" },
                    { 5, "DEAD_LETTERED", "Dead-lettered", "Automatic provider submission cannot continue" },
                    { 6, "AUTOMATIC_RECONCILIATION_STARTED", "Automatic reconciliation started", "A worker acquired a fenced lookup-only reconciliation claim" },
                    { 7, "AUTOMATIC_RECONCILIATION_UNRESOLVED", "Automatic reconciliation unresolved", "Provider lookup was temporarily unavailable" },
                    { 8, "MANUAL_RECONCILIATION_REQUIRED", "Manual reconciliation required", "Automatic evidence was insufficient for a safe decision" },
                    { 9, "RECONCILED_PROVIDER_QUEUED", "Reconciled provider queued", "Exact provider evidence proved acceptance" },
                    { 10, "ABANDONED", "Abandoned", "The publication was explicitly abandoned" },
                    { 11, "PROVIDER_ABSENCE_CONFIRMED", "Provider absence confirmed", "Conformance-proven lookup confirmed absence before unchanged-identity retry" }
                });

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_provider_publication_attempts_webhook_provider_publ",
                table: "webhook_provider_publication_attempts",
                column: "outcome_id",
                principalTable: "webhook_provider_publication_attempt_outcomes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_provider_publication_attempts_webhook_provider_publ1",
                table: "webhook_provider_publication_attempts",
                columns: new[] { "tenant_id", "webhook_provider_publication_id" },
                principalTable: "webhook_provider_publications",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_webhook_provider_publication_attempts_webhook_provider_publ",
                table: "webhook_provider_publication_attempts");

            migrationBuilder.DropForeignKey(
                name: "fk_webhook_provider_publication_attempts_webhook_provider_publ1",
                table: "webhook_provider_publication_attempts");

            migrationBuilder.DropTable(
                name: "webhook_provider_publication_attempt_outcomes");

            migrationBuilder.DropIndex(
                name: "ix_webhook_provider_publication_attempts_outcome_id",
                table: "webhook_provider_publication_attempts");

            migrationBuilder.DropIndex(
                name: "ix_webhook_provider_publication_attempts_tenant_outcome_recorded",
                table: "webhook_provider_publication_attempts");

            migrationBuilder.RenameColumn(
                name: "outcome_id",
                table: "webhook_provider_publication_attempts",
                newName: "outcome");

            migrationBuilder.AddForeignKey(
                name: "fk_webhook_provider_publication_attempts_webhook_provider_publ",
                table: "webhook_provider_publication_attempts",
                columns: new[] { "tenant_id", "webhook_provider_publication_id" },
                principalTable: "webhook_provider_publications",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
