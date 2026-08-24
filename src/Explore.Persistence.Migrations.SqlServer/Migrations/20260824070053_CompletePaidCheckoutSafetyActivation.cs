using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class CompletePaidCheckoutSafetyActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_reconciliation_effects_status_next_attempt_at_created_at",
                schema: "islamu_event",
                table: "payment_reconciliation_effects");

            migrationBuilder.AddColumn<Guid>(
                name: "paid_order_acceptance_snapshot_id",
                schema: "islamu_event",
                table: "payment_attempts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "per_event_sales_count_ceiling",
                schema: "islamu_event",
                table: "paid_event_policy_currency_risk_limits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rolling_organizer_sales_count_ceiling",
                schema: "islamu_event",
                table: "paid_event_policy_currency_risk_limits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rolling_organizer_window_days",
                schema: "islamu_event",
                table: "paid_event_policy_currency_risk_limits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "paid_checkout_review_approvals",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organizer_actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    paid_event_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    trigger_id = table.Column<int>(type: "int", nullable: false),
                    maximum_order_amount_minor = table.Column<long>(type: "bigint", nullable: true),
                    status_code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    request_reason_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    review_reason_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_checkout_review_approvals", x => x.id);
                    table.UniqueConstraint("ak_paid_checkout_review_approvals_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_paid_checkout_review_approvals_amount", "(trigger_id = 1 AND maximum_order_amount_minor IS NULL) OR (trigger_id = 2 AND maximum_order_amount_minor > 0)");
                    table.CheckConstraint("ck_paid_checkout_review_approvals_separation", "reviewed_by_user_id IS NULL OR reviewed_by_user_id <> requested_by_user_id");
                    table.CheckConstraint("ck_paid_checkout_review_approvals_status", "status_code IN ('pending', 'approved', 'rejected')");
                    table.CheckConstraint("ck_paid_checkout_review_approvals_trigger", "trigger_id IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_paid_checkout_review_approvals_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paid_checkout_review_approvals_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paid_checkout_sale_controls",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    scope_key = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    is_stopped = table.Column<bool>(type: "bit", nullable: false),
                    resume_requested_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    resume_requested_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    resume_reviewed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    resume_reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_checkout_sale_controls", x => x.id);
                    table.UniqueConstraint("ak_paid_checkout_sale_controls_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_paid_checkout_sale_controls_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_paid_checkout_sale_controls_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paid_checkout_sale_controls_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paid_order_acceptance_snapshots",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    composition_revision = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    disclosure_revision = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    merchant_disclosure_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    operator_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    operator_display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_official_instance = table.Column<bool>(type: "bit", nullable: false),
                    official_origin = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    operator_region_code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    operator_website_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    operator_legal_notice_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    operator_terms_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    operator_privacy_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    complaint_contact = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    complaint_owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    refund_owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    dispute_owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    reconciliation_owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    activation_status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    delivery_starts_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    delivery_ends_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    event_time_zone_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    total_minor = table.Column<long>(type: "bigint", nullable: false),
                    instance_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    refund_policy_version = table.Column<int>(type: "int", nullable: false),
                    refund_policy_text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    refund_policy_language_tag = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: false),
                    support_contact = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    provider_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    provider_profile_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    charge_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    statement_descriptor = table.Column<string>(type: "nvarchar(22)", maxLength: 22, nullable: false),
                    provider_environment = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    provider_credential_owner = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    accepted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_order_acceptance_snapshots", x => x.id);
                    table.UniqueConstraint("ak_paid_order_acceptance_snapshots_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_paid_order_acceptance_amounts", "organizer_amount_minor > 0 AND platform_fee_minor >= 0 AND platform_fee_minor <= organizer_amount_minor AND platform_contribution_minor >= 0 AND total_minor = organizer_amount_minor + platform_contribution_minor");
                    table.CheckConstraint("ck_paid_order_acceptance_delivery", "delivery_ends_at_utc > delivery_starts_at_utc");
                    table.CheckConstraint("ck_paid_order_acceptance_refund_version", "refund_policy_version > 0");
                    table.ForeignKey(
                        name: "fk_paid_order_acceptance_snapshots_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paid_order_acceptance_snapshots_registration_orders_tenant_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paid_order_acceptance_snapshots_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paid_checkout_sale_control_audits",
                schema: "islamu_event",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    paid_checkout_sale_control_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    action_code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    reason_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subject_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_checkout_sale_control_audits", x => new { x.tenant_id, x.paid_checkout_sale_control_id, x.sequence });
                    table.CheckConstraint("ck_paid_checkout_sale_control_audits_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_paid_checkout_sale_control_audits_paid_checkout_sale_controls_tenant_id_paid_checkout_sale_control_id",
                        columns: x => new { x.tenant_id, x.paid_checkout_sale_control_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_checkout_sale_controls",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paid_order_acceptance_lines",
                schema: "islamu_event",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    discount_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    line_total_minor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_order_acceptance_lines", x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id, x.ordinal });
                    table.CheckConstraint("ck_paid_order_acceptance_lines_shape", "ordinal >= 0 AND quantity > 0 AND unit_amount_minor >= 0 AND discount_amount_minor >= 0 AND line_total_minor >= 0 AND discount_amount_minor <= unit_amount_minor * quantity AND line_total_minor = unit_amount_minor * quantity - discount_amount_minor");
                    table.ForeignKey(
                        name: "fk_paid_order_acceptance_lines_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_order_acceptance_snapshots",
                        principalColumns: new[] { "tenant_id", "id" });
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_expired_lease_poll",
                schema: "islamu_event",
                table: "payment_reconciliation_effects",
                columns: new[] { "status", "processing_lease_expires_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_worker_poll",
                schema: "islamu_event",
                table: "payment_reconciliation_effects",
                columns: new[] { "status", "next_attempt_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_tenant_id_paid_order_acceptance_snapshot_id",
                schema: "islamu_event",
                table: "payment_attempts",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_paid_checkout_review_approvals_tenant_id_event_id_organizer_actor_id_paid_event_policy_version_id_currency_code_trigger_id_s",
                schema: "islamu_event",
                table: "paid_checkout_review_approvals",
                columns: new[] { "tenant_id", "event_id", "organizer_actor_id", "paid_event_policy_version_id", "currency_code", "trigger_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "ix_paid_checkout_sale_control_audits_tenant_id_event_id_occurred_at",
                schema: "islamu_event",
                table: "paid_checkout_sale_control_audits",
                columns: new[] { "tenant_id", "event_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_paid_checkout_sale_controls_tenant_id_event_id",
                schema: "islamu_event",
                table: "paid_checkout_sale_controls",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_paid_checkout_sale_controls_tenant_id_scope_key",
                schema: "islamu_event",
                table: "paid_checkout_sale_controls",
                columns: new[] { "tenant_id", "scope_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_order_acceptance_lines_tenant_id_paid_order_acceptance_snapshot_id_order_line_id",
                schema: "islamu_event",
                table: "paid_order_acceptance_lines",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_order_acceptance_snapshots_tenant_id_event_id_accepted_at",
                schema: "islamu_event",
                table: "paid_order_acceptance_snapshots",
                columns: new[] { "tenant_id", "event_id", "accepted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_paid_order_acceptance_snapshots_tenant_id_registration_order_id_disclosure_revision",
                schema: "islamu_event",
                table: "paid_order_acceptance_snapshots",
                columns: new[] { "tenant_id", "registration_order_id", "disclosure_revision" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_attempts_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                schema: "islamu_event",
                table: "payment_attempts",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" },
                principalSchema: "islamu_event",
                principalTable: "paid_order_acceptance_snapshots",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_attempts_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                schema: "islamu_event",
                table: "payment_attempts");

            migrationBuilder.DropTable(
                name: "paid_checkout_review_approvals",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_checkout_sale_control_audits",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_order_acceptance_lines",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_checkout_sale_controls",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_order_acceptance_snapshots",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ix_payment_reconciliation_effects_expired_lease_poll",
                schema: "islamu_event",
                table: "payment_reconciliation_effects");

            migrationBuilder.DropIndex(
                name: "ix_payment_reconciliation_effects_worker_poll",
                schema: "islamu_event",
                table: "payment_reconciliation_effects");

            migrationBuilder.DropIndex(
                name: "ix_payment_attempts_tenant_id_paid_order_acceptance_snapshot_id",
                schema: "islamu_event",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "paid_order_acceptance_snapshot_id",
                schema: "islamu_event",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "per_event_sales_count_ceiling",
                schema: "islamu_event",
                table: "paid_event_policy_currency_risk_limits");

            migrationBuilder.DropColumn(
                name: "rolling_organizer_sales_count_ceiling",
                schema: "islamu_event",
                table: "paid_event_policy_currency_risk_limits");

            migrationBuilder.DropColumn(
                name: "rolling_organizer_window_days",
                schema: "islamu_event",
                table: "paid_event_policy_currency_risk_limits");

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_status_next_attempt_at_created_at",
                schema: "islamu_event",
                table: "payment_reconciliation_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });
        }
    }
}
