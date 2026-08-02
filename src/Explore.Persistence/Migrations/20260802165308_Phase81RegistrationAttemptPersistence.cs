using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase81RegistrationAttemptPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_registration_channels_tenant_id_event_id_registration_workf",
                table: "registration_channels");

            migrationBuilder.AddColumn<Guid>(
                name: "registration_workflow_version_key",
                table: "registration_orders",
                type: "uuid",
                nullable: false,
                computedColumnSql: "COALESCE(registration_workflow_version_id, '00000000-0000-0000-0000-000000000000'::uuid)",
                stored: true);

            migrationBuilder.AddColumn<Guid>(
                name: "registration_provider_binding_key",
                table: "registration_channels",
                type: "uuid",
                nullable: false,
                computedColumnSql: "COALESCE(registration_provider_binding_id, '00000000-0000-0000-0000-000000000000'::uuid)",
                stored: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_orders_tenant_id_event_id_id",
                table: "registration_orders",
                columns: new[] { "tenant_id", "event_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_orders_tenant_id_event_id_registration_workflo",
                table: "registration_orders",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_version_key", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_channels_tenant_id_event_id_registration_workf",
                table: "registration_channels",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_id", "registration_requirement_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_channels_tenant_id_event_id_registration_workf1",
                table: "registration_channels",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_id", "registration_requirement_id", "id", "registration_provider_binding_key" });

            migrationBuilder.CreateTable(
                name: "registration_attempt_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_attempt_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_submission_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_submission_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registration_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_provider_binding_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_mapping_revision_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    capability_token_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submission_consumption_claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    superseded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    superseded_by_registration_attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supersession_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_provider_binding_key = table.Column<Guid>(type: "uuid", nullable: false, computedColumnSql: "COALESCE(registration_provider_binding_id, '00000000-0000-0000-0000-000000000000'::uuid)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_attempts", x => x.id);
                    table.UniqueConstraint("ak_registration_attempts_tenant_id_event_id_registration_order", x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_workflow_id, x.registration_requirement_id, x.registration_channel_id, x.registration_form_id, x.id });
                    table.UniqueConstraint("ak_registration_attempts_tenant_id_event_id_registration_order1", x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_workflow_id, x.registration_requirement_id, x.registration_channel_id, x.registration_form_id, x.registration_form_version_id, x.id });
                    table.UniqueConstraint("ak_registration_attempts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_attempts_consumption", "(status_id = 2 AND consumed_at IS NOT NULL) OR (status_id <> 2 AND consumed_at IS NULL AND submission_consumption_claim_id IS NULL)");
                    table.CheckConstraint("ck_registration_attempts_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_registration_attempts_provider_key", "(registration_provider_binding_id IS NULL AND registration_provider_binding_key = '00000000-0000-0000-0000-000000000000') OR (registration_provider_binding_id IS NOT NULL AND registration_provider_binding_key = registration_provider_binding_id)");
                    table.CheckConstraint("ck_registration_attempts_provider_pair", "(registration_provider_binding_id IS NULL) = (provider_mapping_revision_hash IS NULL)");
                    table.CheckConstraint("ck_registration_attempts_supersession", "(status_id = 4 AND superseded_at IS NOT NULL AND superseded_by_registration_attempt_id IS NOT NULL AND supersession_reason IS NOT NULL) OR (status_id <> 4 AND superseded_at IS NULL AND superseded_by_registration_attempt_id IS NULL AND supersession_reason IS NULL)");
                    table.ForeignKey(
                        name: "fk_registration_attempts_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_attempt_statuses_status_",
                        column: x => x.status_id,
                        principalTable: "registration_attempt_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_attempts_tenant_id_event",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_workflow_id, x.registration_requirement_id, x.registration_channel_id, x.registration_form_id, x.superseded_by_registration_attempt_id },
                        principalTable: "registration_attempts",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_channels_tenant_id_event",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_workflow_id, x.registration_requirement_id, x.registration_channel_id, x.registration_provider_binding_key },
                        principalTable: "registration_channels",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_workflow_id", "registration_requirement_id", "id", "registration_provider_binding_key" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_form_versions_tenant_id_",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_form_id, x.registration_form_version_id },
                        principalTable: "registration_form_versions",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_form_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_forms_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_form_id },
                        principalTable: "registration_forms",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_orders_tenant_id_event_i",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_workflow_id, x.registration_order_id },
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_workflow_version_key", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_requirements_tenant_id_e",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_workflow_id, x.registration_requirement_id },
                        principalTable: "registration_requirements",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_workflow_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_registration_workflows_tenant_id_even",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_workflow_id },
                        principalTable: "registration_workflows",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_status_at_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    business_deduplication_key = table.Column<string>(type: "character varying(71)", maxLength: 71, nullable: false),
                    received_evidence_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    http_idempotency_key_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    registration_provider_binding_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_mapping_revision_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    provider_submission_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_response_revision = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finalized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_consumption_claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_finalizable = table.Column<bool>(type: "boolean", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_submissions", x => x.id);
                    table.UniqueConstraint("ak_registration_submissions_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.CheckConstraint("ck_registration_submissions_finalization_shape", "(status_id = 3 AND is_finalizable = false AND attempt_consumption_claim_id IS NULL AND finalized_at IS NULL) OR (status_id = 1 AND is_finalizable = true AND attempt_consumption_claim_id IS NOT NULL AND finalized_at IS NULL) OR (status_id = 2 AND is_finalizable = true AND attempt_consumption_claim_id IS NOT NULL AND finalized_at IS NOT NULL)");
                    table.CheckConstraint("ck_registration_submissions_provider_tuple", "(registration_provider_binding_id IS NULL AND provider_mapping_revision_hash IS NULL AND provider_submission_id IS NULL AND provider_response_revision IS NULL) OR (registration_provider_binding_id IS NOT NULL AND provider_mapping_revision_hash IS NOT NULL AND provider_submission_id IS NOT NULL AND provider_response_revision IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_registration_submissions_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_submissions_registration_attempt_statuses_atte",
                        column: x => x.attempt_status_at_receipt_id,
                        principalTable: "registration_attempt_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_submissions_registration_attempts_tenant_id_ev",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id, x.registration_workflow_id, x.registration_requirement_id, x.registration_channel_id, x.registration_form_id, x.registration_form_version_id, x.registration_attempt_id },
                        principalTable: "registration_attempts",
                        principalColumns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "registration_form_version_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_submissions_registration_submission_statuses_s",
                        column: x => x.status_id,
                        principalTable: "registration_submission_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_submissions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_submission_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    received_evidence_hash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    provider_revision_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_submission_revisions", x => x.id);
                    table.CheckConstraint("ck_registration_submission_revisions_number", "revision_number > 0");
                    table.ForeignKey(
                        name: "fk_registration_submission_revisions_registration_submissions_",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_submission_id },
                        principalTable: "registration_submissions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_submission_revisions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_orders_workflow_key",
                table: "registration_orders",
                sql: "(registration_workflow_version_id IS NULL AND registration_workflow_version_key = '00000000-0000-0000-0000-000000000000') OR registration_workflow_version_key = registration_workflow_version_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_channels_provider_shape",
                table: "registration_channels",
                sql: "(is_native = true AND registration_provider_binding_id IS NULL AND registration_provider_binding_key = '00000000-0000-0000-0000-000000000000') OR (is_native = false AND registration_provider_binding_id IS NOT NULL AND registration_provider_binding_key = registration_provider_binding_id)");

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempt_statuses_master_code",
                table: "registration_attempt_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_status_id",
                table: "registration_attempts",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_capability_token_hash",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "capability_token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_form_",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_form_id", "registration_form_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_order",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "superseded_by_registration_attempt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_workf",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_event_id_registration_workf1",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_provider_binding_key" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_attempts_tenant_id_status_id_expires_at",
                table: "registration_attempts",
                columns: new[] { "tenant_id", "status_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_submission_revisions_tenant_id_event_id_regist",
                table: "registration_submission_revisions",
                columns: new[] { "tenant_id", "event_id", "registration_submission_id" });

            migrationBuilder.CreateIndex(
                name: "ux_registration_submission_revisions_submission_revision_number",
                table: "registration_submission_revisions",
                columns: new[] { "tenant_id", "registration_submission_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_submission_statuses_master_code",
                table: "registration_submission_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_submissions_attempt_status_at_receipt_id",
                table: "registration_submissions",
                column: "attempt_status_at_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_submissions_http_idempotency_key_hash",
                table: "registration_submissions",
                column: "http_idempotency_key_hash");

            migrationBuilder.CreateIndex(
                name: "ix_registration_submissions_status_id",
                table: "registration_submissions",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_submissions_tenant_id_event_id_registration_or",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "event_id", "registration_order_id", "registration_workflow_id", "registration_requirement_id", "registration_channel_id", "registration_form_id", "registration_form_version_id", "registration_attempt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_submissions_tenant_id_registration_attempt_id_",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "registration_attempt_id", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ux_registration_submissions_native_identity",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "registration_attempt_id", "business_deduplication_key" },
                unique: true,
                filter: "registration_provider_binding_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_registration_submissions_provider_identity",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_submission_id", "provider_response_revision" },
                unique: true,
                filter: "registration_provider_binding_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_submission_revisions");

            migrationBuilder.DropTable(
                name: "registration_submissions");

            migrationBuilder.DropTable(
                name: "registration_attempts");

            migrationBuilder.DropTable(
                name: "registration_submission_statuses");

            migrationBuilder.DropTable(
                name: "registration_attempt_statuses");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_orders_tenant_id_event_id_id",
                table: "registration_orders");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_orders_tenant_id_event_id_registration_workflo",
                table: "registration_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_orders_workflow_key",
                table: "registration_orders");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_channels_tenant_id_event_id_registration_workf",
                table: "registration_channels");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_channels_tenant_id_event_id_registration_workf1",
                table: "registration_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_channels_provider_shape",
                table: "registration_channels");

            migrationBuilder.DropColumn(
                name: "registration_workflow_version_key",
                table: "registration_orders");

            migrationBuilder.DropColumn(
                name: "registration_provider_binding_key",
                table: "registration_channels");

            migrationBuilder.CreateIndex(
                name: "ix_registration_channels_tenant_id_event_id_registration_workf",
                table: "registration_channels",
                columns: new[] { "tenant_id", "event_id", "registration_workflow_id", "registration_requirement_id" });
        }
    }
}
