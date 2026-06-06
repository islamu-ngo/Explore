using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class domainupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "deduplication_key",
                table: "notifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "actor_subscription_notification_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_subscription_notification_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "actor_subscription_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_subscription_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    blocked_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_message_sequence = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_conversations", x => x.id);
                    table.CheckConstraint("ck_ai_conversations_last_message_sequence_nonnegative", "last_message_sequence >= 0");
                    table.CheckConstraint("ck_ai_conversations_status", "status IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_ai_conversations_actors_actor_id",
                        column: x => x.actor_id,
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ai_conversations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ai_conversations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_fanout_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fanout_kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    notification_entity_type_id = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    cursor_subscriber_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_count = table.Column<int>(type: "integer", nullable: false),
                    created_notification_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_fanout_runs", x => x.id);
                    table.CheckConstraint("ck_notification_fanout_runs_created_count_nonnegative", "created_notification_count >= 0");
                    table.CheckConstraint("ck_notification_fanout_runs_processed_count_nonnegative", "processed_count >= 0");
                    table.CheckConstraint("ck_notification_fanout_runs_status", "status IN ('pending', 'processing', 'completed', 'failed')");
                    table.ForeignKey(
                        name: "fk_notification_fanout_runs_actors_tenant_id_source_actor_id",
                        columns: x => new { x.tenant_id, x.source_actor_id },
                        principalTable: "actors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_fanout_runs_notification_entity_types_notifica",
                        column: x => x.notification_entity_type_id,
                        principalTable: "notification_entity_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_fanout_runs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "actor_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_actor_type_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    notification_level_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    subscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    unsubscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_subscriptions", x => x.id);
                    table.CheckConstraint("ck_actor_subscriptions_notification_level", "notification_level_id IN (1, 2, 3)");
                    table.CheckConstraint("ck_actor_subscriptions_status", "status_id IN (1, 2, 3)");
                    table.CheckConstraint("ck_actor_subscriptions_target_actor_type", "target_actor_type_id IN (2, 4)");
                    table.CheckConstraint("ck_actor_subscriptions_unsubscribed_at", "(status_id = 2 AND unsubscribed_at IS NOT NULL) OR (status_id <> 2)");
                    table.ForeignKey(
                        name: "fk_actor_subscriptions_actor_subscription_notification_levels_",
                        column: x => x.notification_level_id,
                        principalTable: "actor_subscription_notification_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actor_subscriptions_actor_subscription_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "actor_subscription_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actor_subscriptions_actor_types_target_actor_type_id",
                        column: x => x.target_actor_type_id,
                        principalTable: "actor_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actor_subscriptions_actors_tenant_id_target_actor_id",
                        columns: x => new { x.tenant_id, x.target_actor_id },
                        principalTable: "actors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actor_subscriptions_tenant_users_tenant_id_subscriber_tenan",
                        columns: x => new { x.tenant_id, x.subscriber_tenant_user_id },
                        principalTable: "tenant_users",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actor_subscriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actor_subscriptions_users_subscriber_user_id",
                        column: x => x.subscriber_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_conversation_references",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_conversation_references", x => x.id);
                    table.CheckConstraint("ck_ai_conversation_references_kind", "kind IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_ai_conversation_references_ai_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "ai_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_messages", x => x.id);
                    table.CheckConstraint("ck_ai_messages_role", "role IN (1, 2, 3, 4)");
                    table.CheckConstraint("ck_ai_messages_sequence_positive", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_ai_messages_ai_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "ai_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_runs", x => x.id);
                    table.CheckConstraint("ck_ai_runs_status", "status IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "fk_ai_runs_ai_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "ai_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_proposed_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    result_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_proposed_actions", x => x.id);
                    table.CheckConstraint("ck_ai_proposed_actions_kind", "kind IN (1)");
                    table.CheckConstraint("ck_ai_proposed_actions_payload_object", "jsonb_typeof(payload_json) = 'object'");
                    table.CheckConstraint("ck_ai_proposed_actions_status", "status IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "fk_ai_proposed_actions_ai_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "ai_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ai_proposed_actions_ai_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "ai_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ai_tool_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_action_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_tool_executions", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_tool_executions_ai_proposed_actions_proposed_action_id",
                        column: x => x.proposed_action_id,
                        principalTable: "ai_proposed_actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_notifications_tenant_user_deduplication_key",
                table: "notifications",
                columns: new[] { "tenant_id", "user_id", "deduplication_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_actor_subscription_notification_levels_master_code",
                table: "actor_subscription_notification_levels",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_actor_subscription_statuses_master_code",
                table: "actor_subscription_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_actor_subscriptions_fanout_scan",
                table: "actor_subscriptions",
                columns: new[] { "tenant_id", "target_actor_id", "status_id", "notification_level_id" });

            migrationBuilder.CreateIndex(
                name: "ix_actor_subscriptions_notification_level_id",
                table: "actor_subscriptions",
                column: "notification_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_subscriptions_status_id",
                table: "actor_subscriptions",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_subscriptions_subscriber_user",
                table: "actor_subscriptions",
                columns: new[] { "tenant_id", "subscriber_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_actor_subscriptions_subscriber_user_id",
                table: "actor_subscriptions",
                column: "subscriber_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_subscriptions_target_actor_type_id",
                table: "actor_subscriptions",
                column: "target_actor_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_actor_subscriptions_active_row",
                table: "actor_subscriptions",
                columns: new[] { "tenant_id", "subscriber_tenant_user_id", "target_actor_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversation_references_conversation_id",
                table: "ai_conversation_references",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ux_ai_conversation_references_identity",
                table: "ai_conversation_references",
                columns: new[] { "tenant_id", "conversation_id", "kind", "reference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversations_actor_id",
                table: "ai_conversations",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversations_tenant_actor_updated_at",
                table: "ai_conversations",
                columns: new[] { "tenant_id", "actor_id", "updated_at" },
                descending: new[] { false, false, true },
                filter: "actor_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversations_tenant_user_status_updated_at",
                table: "ai_conversations",
                columns: new[] { "tenant_id", "user_id", "status", "updated_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversations_user_id",
                table: "ai_conversations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_messages_conversation_id",
                table: "ai_messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_messages_tenant_conversation_created_at",
                table: "ai_messages",
                columns: new[] { "tenant_id", "conversation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_ai_messages_tenant_conversation_sequence",
                table: "ai_messages",
                columns: new[] { "tenant_id", "conversation_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_proposed_actions_conversation_id",
                table: "ai_proposed_actions",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_proposed_actions_message_id",
                table: "ai_proposed_actions",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_proposed_actions_tenant_conversation_status_created_at",
                table: "ai_proposed_actions",
                columns: new[] { "tenant_id", "conversation_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_proposed_actions_tenant_status_kind_created_at",
                table: "ai_proposed_actions",
                columns: new[] { "tenant_id", "status", "kind", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_runs_conversation_id",
                table: "ai_runs",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_runs_tenant_conversation_queued_at",
                table: "ai_runs",
                columns: new[] { "tenant_id", "conversation_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_runs_tenant_status_queued_at",
                table: "ai_runs",
                columns: new[] { "tenant_id", "status", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tool_executions_proposed_action_id",
                table: "ai_tool_executions",
                column: "proposed_action_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_tool_executions_tenant_action_started_at",
                table: "ai_tool_executions",
                columns: new[] { "tenant_id", "proposed_action_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tool_executions_tenant_tool_started_at",
                table: "ai_tool_executions",
                columns: new[] { "tenant_id", "tool_name", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_runs_notification_entity_type_id",
                table: "notification_fanout_runs",
                column: "notification_entity_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_runs_tenant_id_source_actor_id",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "source_actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_fanout_runs_worker_poll",
                table: "notification_fanout_runs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_fanout_runs_source",
                table: "notification_fanout_runs",
                columns: new[] { "tenant_id", "fanout_kind", "notification_entity_type_id", "entity_id", "source_actor_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actor_subscriptions");

            migrationBuilder.DropTable(
                name: "ai_conversation_references");

            migrationBuilder.DropTable(
                name: "ai_runs");

            migrationBuilder.DropTable(
                name: "ai_tool_executions");

            migrationBuilder.DropTable(
                name: "notification_fanout_runs");

            migrationBuilder.DropTable(
                name: "actor_subscription_notification_levels");

            migrationBuilder.DropTable(
                name: "actor_subscription_statuses");

            migrationBuilder.DropTable(
                name: "ai_proposed_actions");

            migrationBuilder.DropTable(
                name: "ai_messages");

            migrationBuilder.DropTable(
                name: "ai_conversations");

            migrationBuilder.DropIndex(
                name: "ux_notifications_tenant_user_deduplication_key",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "deduplication_key",
                table: "notifications");
        }
    }
}
