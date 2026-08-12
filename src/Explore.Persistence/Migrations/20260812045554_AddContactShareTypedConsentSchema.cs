using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactShareTypedConsentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_events_tenant_id_source_event_",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_users_user_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_tenant_id_source_registration_",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_user_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_eventcontactshareconsents_scope_unique",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_eventcontactshareconsents_user_status",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropColumn(
                name: "email_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_export_items");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "subject_id");

            migrationBuilder.RenameColumn(
                name: "source_registration_order_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "user_subject_id");

            migrationBuilder.RenameColumn(
                name: "source_event_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "registration_purchaser_order_id");

            migrationBuilder.RenameIndex(
                name: "ix_eventcontactshareconsents_recipient_status",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "ix_event_contact_share_consents_recipient_status");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_consents_tenant_id_source_event_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "ix_event_contact_share_consents_tenant_id_registration_purchas");

            migrationBuilder.AddColumn<DateTime>(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_sensitive_answer_values",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_participant_pii",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "islamu_event",
                table: "registration_order_pii",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_order_pii",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "export_purpose_code",
                schema: "islamu_event",
                table: "registration_form_fields",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_exportable",
                schema: "islamu_event",
                table: "registration_form_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_answers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content_hash",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "failed_at",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "failure_category_id",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "included_field_keys_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "policy_version",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "purpose_code",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "requested_field_keys_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "status_id",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "islamu_event",
                table: "event_contact_share_exports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exported_field_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_export_items",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "consent_text_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_stamp",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "guest_contact_order_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "registration_participant_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "subject_type_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_event_contact_share_consents_tenant_id_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "contact_share_consent_subject_types",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_share_consent_subject_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_contact_share_consent_history",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<int>(type: "integer", nullable: false),
                    status_snapshot = table.Column<int>(type: "integer", nullable: false),
                    subject_type_id = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose_code_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    email_normalized_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    consent_text_snapshot = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    consent_ui_version_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_registration_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_contact_share_consent_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consent_history_actors_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "islamu_event",
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consent_history_event_contact_share_con",
                        columns: x => new { x.tenant_id, x.consent_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_contact_share_consents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consent_history_events_tenant_id_source",
                        columns: x => new { x.tenant_id, x.source_event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consent_history_registration_orders_ten",
                        columns: x => new { x.tenant_id, x.source_registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consent_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_contact_share_consent_history_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "islamu_event",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_retention_policies",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: true),
                    is_legal_hold = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_retention_policies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registration_form_fields_retention_policy_id",
                schema: "islamu_event",
                table: "registration_form_fields",
                column: "retention_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_subject_status",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "subject_type_id", "subject_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_subject_type_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                column: "subject_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_tenant_id_guest_contact_order_",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "guest_contact_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_tenant_id_registration_partici",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "registration_participant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_user_subject_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                column: "user_subject_id");

            migrationBuilder.CreateIndex(
                name: "ux_event_contact_share_consents_current_scope",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "subject_type_id", "subject_id", "recipient_actor_id", "purpose_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_contact_share_consents_subject_shape",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                sql: "(CASE WHEN user_subject_id IS NULL THEN 0 ELSE 1 END + CASE WHEN registration_purchaser_order_id IS NULL THEN 0 ELSE 1 END + CASE WHEN registration_participant_id IS NULL THEN 0 ELSE 1 END + CASE WHEN guest_contact_order_id IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "ix_contact_share_consent_subject_types_master_code",
                schema: "islamu_event",
                table: "contact_share_consent_subject_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consent_history_actor_id",
                schema: "islamu_event",
                table: "event_contact_share_consent_history",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consent_history_tenant_id_consent_id_oc",
                schema: "islamu_event",
                table: "event_contact_share_consent_history",
                columns: new[] { "tenant_id", "consent_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consent_history_tenant_id_source_event_",
                schema: "islamu_event",
                table: "event_contact_share_consent_history",
                columns: new[] { "tenant_id", "source_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consent_history_tenant_id_source_regist",
                schema: "islamu_event",
                table: "event_contact_share_consent_history",
                columns: new[] { "tenant_id", "source_registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consent_history_user_id",
                schema: "islamu_event",
                table: "event_contact_share_consent_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_retention_policies_master_code",
                schema: "islamu_event",
                table: "registration_retention_policies",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_contact_share_consent_subject_",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                column: "subject_type_id",
                principalSchema: "islamu_event",
                principalTable: "contact_share_consent_subject_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "guest_contact_order_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_1",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "registration_purchaser_order_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_registration_participants_tena",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "registration_participant_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_participants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_users_user_subject_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                column: "user_subject_id",
                principalSchema: "islamu_event",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_form_fields_registration_retention_policies_re",
                schema: "islamu_event",
                table: "registration_form_fields",
                column: "retention_policy_id",
                principalSchema: "islamu_event",
                principalTable: "registration_retention_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registration_order_pii_tenants_tenant_id",
                schema: "islamu_event",
                table: "registration_order_pii",
                column: "tenant_id",
                principalSchema: "islamu_event",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_contact_share_consent_subject_",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_1",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_registration_participants_tena",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_event_contact_share_consents_users_user_subject_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_form_fields_registration_retention_policies_re",
                schema: "islamu_event",
                table: "registration_form_fields");

            migrationBuilder.DropForeignKey(
                name: "fk_registration_order_pii_tenants_tenant_id",
                schema: "islamu_event",
                table: "registration_order_pii");

            migrationBuilder.DropTable(
                name: "contact_share_consent_subject_types",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "event_contact_share_consent_history",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "registration_retention_policies",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_registration_form_fields_retention_policy_id",
                schema: "islamu_event",
                table: "registration_form_fields");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_event_contact_share_consents_tenant_id_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_subject_status",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_subject_type_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_tenant_id_guest_contact_order_",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_tenant_id_registration_partici",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ix_event_contact_share_consents_user_subject_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropIndex(
                name: "ux_event_contact_share_consents_current_scope",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_event_contact_share_consents_subject_shape",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropColumn(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_sensitive_answer_values");

            migrationBuilder.DropColumn(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_participant_pii");

            migrationBuilder.DropColumn(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_order_pii");

            migrationBuilder.DropColumn(
                name: "export_purpose_code",
                schema: "islamu_event",
                table: "registration_form_fields");

            migrationBuilder.DropColumn(
                name: "is_exportable",
                schema: "islamu_event",
                table: "registration_form_fields");

            migrationBuilder.DropColumn(
                name: "retention_until",
                schema: "islamu_event",
                table: "registration_answers");

            migrationBuilder.DropColumn(
                name: "completed_at",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "content_hash",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "failed_at",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "failure_category_id",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "included_field_keys_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "policy_version",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "purpose_code",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "requested_field_keys_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "status_id",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "islamu_event",
                table: "event_contact_share_exports");

            migrationBuilder.DropColumn(
                name: "exported_field_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_export_items");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropColumn(
                name: "guest_contact_order_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropColumn(
                name: "registration_participant_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.DropColumn(
                name: "subject_type_id",
                schema: "islamu_event",
                table: "event_contact_share_consents");

            migrationBuilder.RenameColumn(
                name: "user_subject_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "source_registration_order_id");

            migrationBuilder.RenameColumn(
                name: "subject_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "registration_purchaser_order_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "source_event_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_consents_tenant_id_registration_purchas",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "ix_event_contact_share_consents_tenant_id_source_event_id");

            migrationBuilder.RenameIndex(
                name: "ix_event_contact_share_consents_recipient_status",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                newName: "ix_eventcontactshareconsents_recipient_status");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "islamu_event",
                table: "registration_order_pii",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_export_items",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "consent_text_snapshot",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_tenant_id_source_registration_",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "source_registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_contact_share_consents_user_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventcontactshareconsents_scope_unique",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "user_id", "recipient_actor_id", "purpose_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_eventcontactshareconsents_user_status",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "user_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_events_tenant_id_source_event_",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "source_event_id" },
                principalSchema: "islamu_event",
                principalTable: "events",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_registration_orders_tenant_id_",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                columns: new[] { "tenant_id", "source_registration_order_id" },
                principalSchema: "islamu_event",
                principalTable: "registration_orders",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_contact_share_consents_users_user_id",
                schema: "islamu_event",
                table: "event_contact_share_consents",
                column: "user_id",
                principalSchema: "islamu_event",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
