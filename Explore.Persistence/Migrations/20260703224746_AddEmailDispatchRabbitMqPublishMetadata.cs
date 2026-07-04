using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDispatchRabbitMqPublishMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "rabbit_mq_last_publish_attempt_at",
                table: "email_dispatch_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rabbit_mq_last_publish_failure_category",
                table: "email_dispatch_outbox",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rabbit_mq_last_published_at",
                table: "email_dispatch_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rabbit_mq_publish_attempt_count",
                table: "email_dispatch_outbox",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_rabbitmq_publish",
                table: "email_dispatch_outbox",
                columns: new[] { "status", "next_attempt_at", "rabbit_mq_last_publish_attempt_at", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_dispatch_outbox_rabbitmq_publish",
                table: "email_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "rabbit_mq_last_publish_attempt_at",
                table: "email_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "rabbit_mq_last_publish_failure_category",
                table: "email_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "rabbit_mq_last_published_at",
                table: "email_dispatch_outbox");

            migrationBuilder.DropColumn(
                name: "rabbit_mq_publish_attempt_count",
                table: "email_dispatch_outbox");
        }
    }
}
