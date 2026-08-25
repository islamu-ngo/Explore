using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationRefundProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "campaign_cursor",
                schema: "islamu_event",
                table: "payment_attempts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_paid_order_acceptance_lines_tenant_id_paid_order_acceptance",
                schema: "islamu_event",
                table: "paid_order_acceptance_lines",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" });

            migrationBuilder.CreateTable(
                name: "payment_disputes",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_dispute_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    stage = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    last_observed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    response_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_disputes", x => x.id);
                    table.UniqueConstraint("ak_payment_disputes_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_payment_disputes_amount", "amount_minor > 0");
                    table.CheckConstraint("ck_payment_disputes_stage", "stage BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_payment_disputes_status", "status BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_payment_disputes_payment_attempts_tenant_id_payment_attempt",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_disputes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refund_campaigns",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    decision_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    decision_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cursor = table.Column<long>(type: "bigint", nullable: false),
                    processing_lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_lease_owner = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    total_payment_count = table.Column<int>(type: "integer", nullable: false),
                    generated_count = table.Column<int>(type: "integer", nullable: false),
                    pending_count = table.Column<int>(type: "integer", nullable: false),
                    succeeded_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    unknown_count = table.Column<int>(type: "integer", nullable: false),
                    operator_case_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refund_campaigns", x => x.id);
                    table.UniqueConstraint("ak_refund_campaigns_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_refund_campaigns_counts", "total_payment_count >= 0 AND generated_count >= 0 AND pending_count >= 0 AND succeeded_count >= 0 AND failed_count >= 0 AND unknown_count >= 0 AND operator_case_count >= 0 AND generated_count <= total_payment_count");
                    table.CheckConstraint("ck_refund_campaigns_cursor", "cursor >= 0");
                    table.CheckConstraint("ck_refund_campaigns_kind", "kind BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_refund_campaigns_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_refund_campaigns_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refund_campaigns_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refund_attempts",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_source_key = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    authority_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    refund_policy_version = table.Column<int>(type: "integer", nullable: false),
                    refund_policy_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    refund_policy_language_tag = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    provider_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    external_account_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    provider_payment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    allocation_organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    allocation_platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    allocation_platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    allocation_total_minor = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    provider_refund_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_provider_request_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    buyer_refund_succeeded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    application_fee_refunded_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    last_observed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    succeeded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refund_attempts", x => x.id);
                    table.UniqueConstraint("ak_refund_attempts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_refund_attempts_allocation", "allocation_organizer_amount_minor >= 0 AND allocation_platform_fee_minor >= 0 AND allocation_platform_contribution_minor >= 0 AND allocation_total_minor > 0 AND allocation_platform_fee_minor <= allocation_organizer_amount_minor AND allocation_total_minor = allocation_organizer_amount_minor + allocation_platform_contribution_minor");
                    table.CheckConstraint("ck_refund_attempts_buyer_success_capacity", "buyer_refund_succeeded_at IS NULL OR status NOT IN (7, 8)");
                    table.CheckConstraint("ck_refund_attempts_fee_refund", "application_fee_refunded_amount_minor >= 0");
                    table.CheckConstraint("ck_refund_attempts_policy_version", "refund_policy_version > 0");
                    table.CheckConstraint("ck_refund_attempts_status", "status BETWEEN 1 AND 8");
                    table.ForeignKey(
                        name: "fk_refund_attempts_paid_order_acceptance_snapshots_tenant_id_p",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_order_acceptance_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refund_attempts_payment_attempts_tenant_id_payment_attempt_",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refund_attempts_refund_campaigns_tenant_id_source_campaign_",
                        columns: x => new { x.tenant_id, x.source_campaign_id },
                        principalSchema: "islamu_event",
                        principalTable: "refund_campaigns",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refund_attempts_registration_orders_tenant_id_registration_",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refund_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registration_material_change_choices",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_material_change_choices", x => x.id);
                    table.UniqueConstraint("ak_registration_material_change_choices_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_registration_material_change_choices_paid_order_acceptance_",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_order_acceptance_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_material_change_choices_payment_attempts_tenan",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_material_change_choices_refund_campaigns_tenan",
                        columns: x => new { x.tenant_id, x.refund_campaign_id },
                        principalSchema: "islamu_event",
                        principalTable: "refund_campaigns",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refund_line_allocations",
                schema: "islamu_event",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    total_minor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refund_line_allocations", x => new { x.tenant_id, x.refund_attempt_id, x.order_line_id });
                    table.CheckConstraint("ck_refund_line_allocations_money", "ordinal >= 0 AND organizer_amount_minor >= 0 AND platform_fee_minor >= 0 AND platform_contribution_minor >= 0 AND total_minor >= 0 AND platform_fee_minor <= organizer_amount_minor AND total_minor = organizer_amount_minor + platform_contribution_minor");
                    table.ForeignKey(
                        name: "fk_refund_line_allocations_paid_order_acceptance_lines_tenant_",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id, x.order_line_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_order_acceptance_lines",
                        principalColumns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refund_line_allocations_refund_attempts_tenant_id_refund_at",
                        columns: x => new { x.tenant_id, x.refund_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "refund_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_tenant_id_campaign_cursor",
                schema: "islamu_event",
                table: "payment_attempts",
                columns: new[] { "tenant_id", "campaign_cursor" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_attempts_campaign_cursor",
                schema: "islamu_event",
                table: "payment_attempts",
                sql: "campaign_cursor > 0");

            migrationBuilder.CreateIndex(
                name: "ix_payment_disputes_tenant_id_payment_attempt_id_status",
                schema: "islamu_event",
                table: "payment_disputes",
                columns: new[] { "tenant_id", "payment_attempt_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_disputes_tenant_id_provider_dispute_id",
                schema: "islamu_event",
                table: "payment_disputes",
                columns: new[] { "tenant_id", "provider_dispute_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_tenant_id_paid_order_acceptance_snapshot_id",
                schema: "islamu_event",
                table: "refund_attempts",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_tenant_id_payment_attempt_id_status",
                schema: "islamu_event",
                table: "refund_attempts",
                columns: new[] { "tenant_id", "payment_attempt_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_tenant_id_provider_code_external_account_id",
                schema: "islamu_event",
                table: "refund_attempts",
                columns: new[] { "tenant_id", "provider_code", "external_account_id", "provider_refund_id" });

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_tenant_id_provider_idempotency_key",
                schema: "islamu_event",
                table: "refund_attempts",
                columns: new[] { "tenant_id", "provider_idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_tenant_id_registration_order_id",
                schema: "islamu_event",
                table: "refund_attempts",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_tenant_id_reservation_source_key_payment_at",
                schema: "islamu_event",
                table: "refund_attempts",
                columns: new[] { "tenant_id", "reservation_source_key", "payment_attempt_id", "paid_order_acceptance_snapshot_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_tenant_id_source_campaign_id_status",
                schema: "islamu_event",
                table: "refund_attempts",
                columns: new[] { "tenant_id", "source_campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_refund_campaigns_status_processing_lease_expires_at_decisio",
                schema: "islamu_event",
                table: "refund_campaigns",
                columns: new[] { "status", "processing_lease_expires_at", "decision_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_refund_campaigns_tenant_id_event_id_kind_decision_at",
                schema: "islamu_event",
                table: "refund_campaigns",
                columns: new[] { "tenant_id", "event_id", "kind", "decision_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refund_line_allocations_tenant_id_paid_order_acceptance_sna",
                schema: "islamu_event",
                table: "refund_line_allocations",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_refund_line_allocations_tenant_id_refund_attempt_id_ordinal",
                schema: "islamu_event",
                table: "refund_line_allocations",
                columns: new[] { "tenant_id", "refund_attempt_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_material_change_choices_tenant_id_paid_order_a",
                schema: "islamu_event",
                table: "registration_material_change_choices",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_material_change_choices_tenant_id_payment_atte",
                schema: "islamu_event",
                table: "registration_material_change_choices",
                columns: new[] { "tenant_id", "payment_attempt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_material_change_choices_tenant_id_refund_campa",
                schema: "islamu_event",
                table: "registration_material_change_choices",
                columns: new[] { "tenant_id", "refund_campaign_id", "payment_attempt_id", "paid_order_acceptance_snapshot_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registration_material_change_choices_tenant_id_registration",
                schema: "islamu_event",
                table: "registration_material_change_choices",
                columns: new[] { "tenant_id", "registration_order_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_disputes",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "refund_line_allocations",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "registration_material_change_choices",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "refund_attempts",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "refund_campaigns",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_payment_attempts_tenant_id_campaign_cursor",
                schema: "islamu_event",
                table: "payment_attempts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_attempts_campaign_cursor",
                schema: "islamu_event",
                table: "payment_attempts");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_paid_order_acceptance_lines_tenant_id_paid_order_acceptance",
                schema: "islamu_event",
                table: "paid_order_acceptance_lines");

            migrationBuilder.DropColumn(
                name: "campaign_cursor",
                schema: "islamu_event",
                table: "payment_attempts");
        }
    }
}
