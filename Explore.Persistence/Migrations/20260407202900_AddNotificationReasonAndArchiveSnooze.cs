using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReasonAndArchiveSnooze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "notification_reason_id",
                table: "notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snoozed_until",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_reasons", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_notification_reason_id",
                table: "notifications",
                column: "notification_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_archived",
                table: "notifications",
                columns: new[] { "user_id", "is_archived", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_notification_reasons_notification_reason_id",
                table: "notifications",
                column: "notification_reason_id",
                principalTable: "notification_reasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notifications_notification_reasons_notification_reason_id",
                table: "notifications");

            migrationBuilder.DropTable(
                name: "notification_reasons");

            migrationBuilder.DropIndex(
                name: "ix_notifications_notification_reason_id",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_notifications_user_archived",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "notification_reason_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "snoozed_until",
                table: "notifications");
        }
    }
}
