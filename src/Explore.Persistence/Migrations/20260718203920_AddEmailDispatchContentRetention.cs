// ABOUTME: Adds the durable email dispatch content-redaction timestamp.
// ABOUTME: Creates the bounded retention index and database redaction safety fence.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDispatchContentRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "content_redacted_at",
                table: "email_dispatch_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_retention",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "content_redacted_at", "status", "sent_at", "last_failure_at", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_email_dispatch_outbox_redaction_fence",
                table: "email_dispatch_outbox",
                sql: "content_redacted_at IS NULL OR (recipient_email = '' AND subject = '' AND plain_text_body IS NULL AND html_body IS NULL AND reply_to IS NULL AND last_error IS NULL AND provider_message_id IS NULL AND correlation_id IS NULL AND next_attempt_at IS NULL AND processing_started_at IS NULL AND processing_lease_token IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_dispatch_outbox_retention",
                table: "email_dispatch_outbox");

            migrationBuilder.DropCheckConstraint(
                name: "ck_email_dispatch_outbox_redaction_fence",
                table: "email_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "content_redacted_at",
                table: "email_dispatch_outbox");
        }
    }
}
