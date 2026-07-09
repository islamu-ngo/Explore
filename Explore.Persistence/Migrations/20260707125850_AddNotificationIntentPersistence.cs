using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationIntentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_workflow_provider_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_workflow_provider_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_delivery_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_delivery_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_external_delegation_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_external_delegation_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_intent_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_intent_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_ownership_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_ownership_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_recipient_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_recipient_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    ownership_type_id = table.Column<int>(type: "integer", nullable: false),
                    recipient_kind_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    template_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    safe_payload_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    safe_payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    report_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_notification_intents", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_intents_event_report_decisions_report_decision",
                        column: x => x.report_decision_id,
                        principalTable: "event_report_decisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_event_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "event_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_notification_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "notification_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_notification_intent_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "notification_intent_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_notification_ownership_types_ownership",
                        column: x => x.ownership_type_id,
                        principalTable: "notification_ownership_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_notification_recipient_kinds_recipient",
                        column: x => x.recipient_kind_id,
                        principalTable: "notification_recipient_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_intents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_dispatch_outbox_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    provider_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_email_dispatch_outbox_email_dispatc",
                        column: x => x.email_dispatch_outbox_id,
                        principalTable: "email_dispatch_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_notification_delivery_statuses_stat",
                        column: x => x.status_id,
                        principalTable: "notification_delivery_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_notification_intents_notification_i",
                        column: x => x.notification_intent_id,
                        principalTable: "notification_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_external_delegations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_kind_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    recipient_kind_id = table.Column<int>(type: "integer", nullable: false),
                    template_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    safe_payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    external_provider_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    external_correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    external_delivery_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    failure_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    report_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_external_delegations", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_external_delegations_event_report_decisions_re",
                        column: x => x.report_decision_id,
                        principalTable: "event_report_decisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_external_delegations_event_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "event_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_external_delegations_external_workflow_provide",
                        column: x => x.provider_kind_id,
                        principalTable: "external_workflow_provider_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_external_delegations_notification_external_del",
                        column: x => x.status_id,
                        principalTable: "notification_external_delegation_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_external_delegations_notification_intents_noti",
                        column: x => x.notification_intent_id,
                        principalTable: "notification_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_external_delegations_notification_recipient_ki",
                        column: x => x.recipient_kind_id,
                        principalTable: "notification_recipient_kinds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_external_delegations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_external_workflow_provider_kinds_master_code",
                table: "external_workflow_provider_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_categories_master_code",
                table: "notification_categories",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_email_dispatch_outbox_id",
                table: "notification_deliveries",
                column: "email_dispatch_outbox_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_notification_intent_id",
                table: "notification_deliveries",
                column: "notification_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_status_id",
                table: "notification_deliveries",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_tenant_email_dispatch_outbox",
                table: "notification_deliveries",
                columns: new[] { "tenant_id", "email_dispatch_outbox_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_tenant_intent",
                table: "notification_deliveries",
                columns: new[] { "tenant_id", "notification_intent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_tenant_status_created",
                table: "notification_deliveries",
                columns: new[] { "tenant_id", "status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_delivery_statuses_master_code",
                table: "notification_delivery_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_external_delegation_statuses_master_code",
                table: "notification_external_delegation_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_notification_intent_id",
                table: "notification_external_delegations",
                column: "notification_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_provider_kind_id",
                table: "notification_external_delegations",
                column: "provider_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_recipient_kind_id",
                table: "notification_external_delegations",
                column: "recipient_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_report_decision_id",
                table: "notification_external_delegations",
                column: "report_decision_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_report_id",
                table: "notification_external_delegations",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_status_id",
                table: "notification_external_delegations",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_tenant_external_correlation",
                table: "notification_external_delegations",
                columns: new[] { "tenant_id", "external_correlation_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_tenant_intent",
                table: "notification_external_delegations",
                columns: new[] { "tenant_id", "notification_intent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_external_delegations_tenant_provider_status",
                table: "notification_external_delegations",
                columns: new[] { "tenant_id", "provider_kind_id", "status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_intent_statuses_master_code",
                table: "notification_intent_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_category_id",
                table: "notification_intents",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_event_id",
                table: "notification_intents",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_ownership_type_id",
                table: "notification_intents",
                column: "ownership_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_recipient_kind_id",
                table: "notification_intents",
                column: "recipient_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_report_decision_id",
                table: "notification_intents",
                column: "report_decision_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_report_id",
                table: "notification_intents",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_status_id",
                table: "notification_intents",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_tenant_category_created",
                table: "notification_intents",
                columns: new[] { "tenant_id", "category_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_tenant_owner_created",
                table: "notification_intents",
                columns: new[] { "tenant_id", "ownership_type_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_tenant_status_created",
                table: "notification_intents",
                columns: new[] { "tenant_id", "status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_intents_user_id",
                table: "notification_intents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_intents_tenant_deduplication_key",
                table: "notification_intents",
                columns: new[] { "tenant_id", "deduplication_key" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_notification_ownership_types_master_code",
                table: "notification_ownership_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_recipient_kinds_master_code",
                table: "notification_recipient_kinds",
                column: "master_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notification_external_delegations");

            migrationBuilder.DropTable(
                name: "notification_delivery_statuses");

            migrationBuilder.DropTable(
                name: "external_workflow_provider_kinds");

            migrationBuilder.DropTable(
                name: "notification_external_delegation_statuses");

            migrationBuilder.DropTable(
                name: "notification_intents");

            migrationBuilder.DropTable(
                name: "notification_categories");

            migrationBuilder.DropTable(
                name: "notification_intent_statuses");

            migrationBuilder.DropTable(
                name: "notification_ownership_types");

            migrationBuilder.DropTable(
                name: "notification_recipient_kinds");
        }
    }
}
