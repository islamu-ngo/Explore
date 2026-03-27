using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class backgroundfield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "background_image_id",
                table: "actors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    aggregate_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_actors_background_image_id",
                table: "actors",
                column: "background_image_id");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Aggregate",
                table: "outbox_messages",
                columns: new[] { "aggregate_type", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Dedup",
                table: "outbox_messages",
                columns: new[] { "aggregate_type", "aggregate_id", "event_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_WorkerPoll",
                table: "outbox_messages",
                columns: new[] { "status", "next_retry_at", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_actors_storage_objects_background_image_id",
                table: "actors",
                column: "background_image_id",
                principalTable: "storage_objects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_actors_storage_objects_background_image_id",
                table: "actors");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_actors_background_image_id",
                table: "actors");

            migrationBuilder.DropColumn(
                name: "background_image_id",
                table: "actors");
        }
    }
}
