// ABOUTME: Adds durable global and per-tenant SMTP admission state for atomic cross-replica dispatch claims.
// ABOUTME: Enforces paired refill metadata and nonnegative token counts while keeping the migration reversible.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDispatchAtomicClaimState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "smtp_available_tokens",
                table: "email_dispatch_tenant_controls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "smtp_refill_at",
                table: "email_dispatch_tenant_controls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_dispatch_processor_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    processor_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    optional_reminders_deferred = table.Column<bool>(type: "boolean", nullable: false),
                    smtp_available_tokens = table.Column<int>(type: "integer", nullable: true),
                    smtp_refill_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_dispatch_processor_states", x => x.id);
                    table.CheckConstraint(
                        "ck_email_dispatch_processor_states_smtp_rate_pair",
                        "(smtp_available_tokens IS NULL) = (smtp_refill_at IS NULL)");
                    table.CheckConstraint(
                        "ck_email_dispatch_processor_states_smtp_tokens_nonnegative",
                        "smtp_available_tokens IS NULL OR smtp_available_tokens >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_processor_states_processor_code",
                table: "email_dispatch_processor_states",
                column: "processor_code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_rate_pair",
                table: "email_dispatch_tenant_controls",
                sql: "(smtp_available_tokens IS NULL) = (smtp_refill_at IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_tokens_nonnegative",
                table: "email_dispatch_tenant_controls",
                sql: "smtp_available_tokens IS NULL OR smtp_available_tokens >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_dispatch_processor_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_rate_pair",
                table: "email_dispatch_tenant_controls");

            migrationBuilder.DropCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_tokens_nonnegative",
                table: "email_dispatch_tenant_controls");

            migrationBuilder.DropColumn(
                name: "smtp_available_tokens",
                table: "email_dispatch_tenant_controls");

            migrationBuilder.DropColumn(
                name: "smtp_refill_at",
                table: "email_dispatch_tenant_controls");
        }
    }
}
