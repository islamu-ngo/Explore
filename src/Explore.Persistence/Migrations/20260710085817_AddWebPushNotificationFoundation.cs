// ABOUTME: EF Core migration adding Web Push preference metadata, subscriptions, and dispatch outbox tables.
// ABOUTME: Keeps the Wave 1 browser push schema additive and reversible for PostgreSQL deployments.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebPushNotificationFoundation : Migration
    {
        private static readonly string[] WebPushDispatchOutboxTenantStatusColumns = ["tenant_id", "status", "last_failure_at"];
        private static readonly string[] WebPushDispatchOutboxWorkerPollColumns = ["status", "next_attempt_at", "created_at"];
        private static readonly string[] WebPushDispatchOutboxNotificationSubscriptionColumns = ["tenant_id", "notification_id", "subscription_id"];
        private static readonly string[] WebPushSubscriptionsTenantUserActiveColumns = ["tenant_id", "user_id", "is_active"];
        private static readonly string[] WebPushSubscriptionsActiveUserDeviceColumns = ["tenant_id", "user_id", "device_identifier"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "default_push_enabled",
                table: "notification_preference_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE notification_preference_categories
                SET default_push_enabled = default_in_app_enabled
                WHERE master_code <> 'MARKETING';
                """);

            migrationBuilder.CreateTable(
                name: "web_push_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    p256dh = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    auth_secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unsubscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivation_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_web_push_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_web_push_subscriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_web_push_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "web_push_dispatch_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    skipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    permanent_failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_web_push_dispatch_outbox", x => x.id);
                    table.ForeignKey(
                        name: "fk_web_push_dispatch_outbox_notification_preference_categories",
                        column: x => x.category_id,
                        principalTable: "notification_preference_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_web_push_dispatch_outbox_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_web_push_dispatch_outbox_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_web_push_dispatch_outbox_web_push_subscriptions_subscriptio",
                        column: x => x.subscription_id,
                        principalTable: "web_push_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_web_push_dispatch_outbox_category_id",
                table: "web_push_dispatch_outbox",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_web_push_dispatch_outbox_subscription_id",
                table: "web_push_dispatch_outbox",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_web_push_dispatch_outbox_tenant_status",
                table: "web_push_dispatch_outbox",
                columns: WebPushDispatchOutboxTenantStatusColumns);

            migrationBuilder.CreateIndex(
                name: "ix_web_push_dispatch_outbox_user_id",
                table: "web_push_dispatch_outbox",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_web_push_dispatch_outbox_worker_poll",
                table: "web_push_dispatch_outbox",
                columns: WebPushDispatchOutboxWorkerPollColumns);

            migrationBuilder.CreateIndex(
                name: "ux_web_push_dispatch_outbox_notification_subscription",
                table: "web_push_dispatch_outbox",
                columns: WebPushDispatchOutboxNotificationSubscriptionColumns,
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_web_push_subscriptions_tenant_user_active",
                table: "web_push_subscriptions",
                columns: WebPushSubscriptionsTenantUserActiveColumns);

            migrationBuilder.CreateIndex(
                name: "ix_web_push_subscriptions_user_id",
                table: "web_push_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_web_push_subscriptions_active_endpoint",
                table: "web_push_subscriptions",
                column: "endpoint",
                unique: true,
                filter: "is_deleted = false AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "ux_web_push_subscriptions_active_user_device",
                table: "web_push_subscriptions",
                columns: WebPushSubscriptionsActiveUserDeviceColumns,
                unique: true,
                filter: "is_deleted = false AND is_active = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "web_push_dispatch_outbox");

            migrationBuilder.DropTable(
                name: "web_push_subscriptions");

            migrationBuilder.DropColumn(
                name: "default_push_enabled",
                table: "notification_preference_categories");
        }
    }
}
