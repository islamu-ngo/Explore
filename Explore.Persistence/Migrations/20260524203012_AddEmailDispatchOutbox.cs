using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDispatchOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_dispatch_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publish_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    source_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    plain_text_body = table.Column<string>(type: "text", nullable: true),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    reply_to = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    parked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unknown_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_dispatch_outbox", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_event_registration_intents_registrati",
                        column: x => x.registration_intent_id,
                        principalTable: "event_registration_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_email_dispatch_outbox_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_dispatch_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_dispatch_outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    transport = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sanitized_error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_dispatch_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_dispatch_attempts_email_dispatch_outbox_email_dispatc",
                        column: x => x.email_dispatch_outbox_id,
                        principalTable: "email_dispatch_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_email_dispatch_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_dispatch_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    publish_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_dispatch_outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    consumer_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_dispatch_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_dispatch_receipts_email_dispatch_outbox_email_dispatc",
                        column: x => x.email_dispatch_outbox_id,
                        principalTable: "email_dispatch_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_email_dispatch_receipts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_attempts_tenant_started",
                table: "email_dispatch_attempts",
                columns: new[] { "tenant_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_attempts_outbox_attempt",
                table: "email_dispatch_attempts",
                columns: new[] { "email_dispatch_outbox_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_event_id",
                table: "email_dispatch_outbox",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_registration_intent_id",
                table: "email_dispatch_outbox",
                column: "registration_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_tenant_status",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "status", "last_failure_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_user_id",
                table: "email_dispatch_outbox",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_outbox_worker_poll",
                table: "email_dispatch_outbox",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_outbox_tenant_publish_event",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "publish_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_outbox_tenant_source_kind",
                table: "email_dispatch_outbox",
                columns: new[] { "tenant_id", "source_type", "source_id", "kind" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_receipts_outbox_status",
                table: "email_dispatch_receipts",
                columns: new[] { "email_dispatch_outbox_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_email_dispatch_receipts_tenant_publish_event",
                table: "email_dispatch_receipts",
                columns: new[] { "tenant_id", "publish_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_dispatch_attempts");

            migrationBuilder.DropTable(
                name: "email_dispatch_receipts");

            migrationBuilder.DropTable(
                name: "email_dispatch_outbox");
        }
    }
}
