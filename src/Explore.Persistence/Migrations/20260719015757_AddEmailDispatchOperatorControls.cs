// ABOUTME: Adds durable global pause and SMTP rate override controls to the email dispatch processor state.
// ABOUTME: Records bounded operator audit metadata and reverses the schema changes without data-dependent SQL.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDispatchOperatorControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "email_dispatch_processor_states",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pause_reason",
                table: "email_dispatch_processor_states",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "paused_at",
                table: "email_dispatch_processor_states",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "paused_by",
                table: "email_dispatch_processor_states",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "global_smtp_rate_limit_per_minute_override",
                table: "email_dispatch_processor_states",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "email_dispatch_processor_states",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_email_dispatch_processor_states_global_rate_override",
                table: "email_dispatch_processor_states",
                sql: "global_smtp_rate_limit_per_minute_override IS NULL OR global_smtp_rate_limit_per_minute_override BETWEEN 1 AND 100000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_email_dispatch_processor_states_global_rate_override",
                table: "email_dispatch_processor_states");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "email_dispatch_processor_states");

            migrationBuilder.DropColumn(
                name: "global_smtp_rate_limit_per_minute_override",
                table: "email_dispatch_processor_states");

            migrationBuilder.DropColumn(
                name: "paused_by",
                table: "email_dispatch_processor_states");

            migrationBuilder.DropColumn(
                name: "paused_at",
                table: "email_dispatch_processor_states");

            migrationBuilder.DropColumn(
                name: "pause_reason",
                table: "email_dispatch_processor_states");

            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "email_dispatch_processor_states");
        }
    }
}
