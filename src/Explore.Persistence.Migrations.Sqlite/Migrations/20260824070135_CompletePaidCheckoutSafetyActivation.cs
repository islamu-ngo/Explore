using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class CompletePaidCheckoutSafetyActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ie_payment_reconciliation_effects_status_next_attempt_at_created_at",
                table: "ie_payment_reconciliation_effects");

            migrationBuilder.AddColumn<Guid>(
                name: "paid_order_acceptance_snapshot_id",
                table: "ie_payment_attempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "per_event_sales_count_ceiling",
                table: "ie_paid_event_policy_currency_risk_limits",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rolling_organizer_sales_count_ceiling",
                table: "ie_paid_event_policy_currency_risk_limits",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rolling_organizer_window_days",
                table: "ie_paid_event_policy_currency_risk_limits",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ie_paid_checkout_review_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    organizer_actor_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    paid_event_policy_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    trigger_id = table.Column<int>(type: "INTEGER", nullable: false),
                    maximum_order_amount_minor = table.Column<long>(type: "INTEGER", nullable: true),
                    status_code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    request_reason_code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    requested_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    review_reason_code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_paid_checkout_review_approvals", x => x.id);
                    table.UniqueConstraint("ak_paid_checkout_review_approvals_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_paid_checkout_review_approvals_amount", "(trigger_id = 1 AND maximum_order_amount_minor IS NULL) OR (trigger_id = 2 AND maximum_order_amount_minor > 0)");
                    table.CheckConstraint("ck_paid_checkout_review_approvals_separation", "reviewed_by_user_id IS NULL OR reviewed_by_user_id <> requested_by_user_id");
                    table.CheckConstraint("ck_paid_checkout_review_approvals_status", "status_code IN ('pending', 'approved', 'rejected')");
                    table.CheckConstraint("ck_paid_checkout_review_approvals_trigger", "trigger_id IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_ie_paid_checkout_review_approvals_ie_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_paid_checkout_review_approvals_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_paid_checkout_sale_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    scope_key = table.Column<string>(type: "TEXT", maxLength: 48, nullable: false),
                    is_stopped = table.Column<bool>(type: "INTEGER", nullable: false),
                    resume_requested_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    resume_requested_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    resume_reviewed_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    resume_reviewed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    version = table.Column<long>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_paid_checkout_sale_controls", x => x.id);
                    table.UniqueConstraint("ak_paid_checkout_sale_controls_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_paid_checkout_sale_controls_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_ie_paid_checkout_sale_controls_ie_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_paid_checkout_sale_controls_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_paid_order_acceptance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    composition_revision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    disclosure_revision = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    merchant_disclosure_text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    operator_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operator_display_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    is_official_instance = table.Column<bool>(type: "INTEGER", nullable: false),
                    official_origin = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    operator_region_code = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    operator_website_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    operator_legal_notice_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    operator_terms_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    operator_privacy_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    complaint_contact = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    complaint_owner = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    refund_owner = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    dispute_owner = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    reconciliation_owner = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    activation_status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    delivery_starts_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    delivery_ends_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    event_time_zone_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    currency_code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    organizer_amount_minor = table.Column<long>(type: "INTEGER", nullable: false),
                    platform_fee_minor = table.Column<long>(type: "INTEGER", nullable: false),
                    platform_contribution_minor = table.Column<long>(type: "INTEGER", nullable: false),
                    total_minor = table.Column<long>(type: "INTEGER", nullable: false),
                    instance_policy_version_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_policy_version_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    refund_policy_version = table.Column<int>(type: "INTEGER", nullable: false),
                    refund_policy_text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    refund_policy_language_tag = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    support_contact = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    provider_code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    provider_profile_code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    charge_type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    statement_descriptor = table.Column<string>(type: "TEXT", maxLength: 22, nullable: false),
                    provider_environment = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    provider_credential_owner = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    accepted_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_paid_order_acceptance_snapshots", x => x.id);
                    table.UniqueConstraint("ak_paid_order_acceptance_snapshots_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_paid_order_acceptance_amounts", "organizer_amount_minor > 0 AND platform_fee_minor >= 0 AND platform_fee_minor <= organizer_amount_minor AND platform_contribution_minor >= 0 AND total_minor = organizer_amount_minor + platform_contribution_minor");
                    table.CheckConstraint("ck_paid_order_acceptance_delivery", "delivery_ends_at_utc > delivery_starts_at_utc");
                    table.CheckConstraint("ck_paid_order_acceptance_refund_version", "refund_policy_version > 0");
                    table.ForeignKey(
                        name: "fk_ie_paid_order_acceptance_snapshots_ie_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_paid_order_acceptance_snapshots_registration_orders_tenant_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_paid_order_acceptance_snapshots_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_paid_checkout_sale_control_audits",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    paid_checkout_sale_control_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    action_code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    reason_code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    subject_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_paid_checkout_sale_control_audits", x => new { x.tenant_id, x.paid_checkout_sale_control_id, x.sequence });
                    table.CheckConstraint("ck_paid_checkout_sale_control_audits_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_ie_paid_checkout_sale_control_audits_ie_paid_checkout_sale_controls_tenant_id_paid_checkout_sale_control_id",
                        columns: x => new { x.tenant_id, x.paid_checkout_sale_control_id },
                        principalTable: "ie_paid_checkout_sale_controls",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ie_paid_order_acceptance_lines",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    order_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    unit_amount_minor = table.Column<long>(type: "INTEGER", nullable: false),
                    discount_amount_minor = table.Column<long>(type: "INTEGER", nullable: false),
                    line_total_minor = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_paid_order_acceptance_lines", x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id, x.ordinal });
                    table.CheckConstraint("ck_paid_order_acceptance_lines_shape", "ordinal >= 0 AND quantity > 0 AND unit_amount_minor >= 0 AND discount_amount_minor >= 0 AND line_total_minor >= 0 AND discount_amount_minor <= unit_amount_minor * quantity AND line_total_minor = unit_amount_minor * quantity - discount_amount_minor");
                    table.ForeignKey(
                        name: "fk_ie_paid_order_acceptance_lines_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id },
                        principalTable: "ie_paid_order_acceptance_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_expired_lease_poll",
                table: "ie_payment_reconciliation_effects",
                columns: new[] { "status", "processing_lease_expires_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_effects_worker_poll",
                table: "ie_payment_reconciliation_effects",
                columns: new[] { "status", "next_attempt_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_attempts_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_payment_attempts",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_checkout_review_approvals_tenant_id_event_id_organizer_actor_id_paid_event_policy_version_id_currency_code_trigger_id_status_code",
                table: "ie_paid_checkout_review_approvals",
                columns: new[] { "tenant_id", "event_id", "organizer_actor_id", "paid_event_policy_version_id", "currency_code", "trigger_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_checkout_sale_control_audits_tenant_id_event_id_occurred_at",
                table: "ie_paid_checkout_sale_control_audits",
                columns: new[] { "tenant_id", "event_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_checkout_sale_controls_tenant_id_event_id",
                table: "ie_paid_checkout_sale_controls",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_checkout_sale_controls_tenant_id_scope_key",
                table: "ie_paid_checkout_sale_controls",
                columns: new[] { "tenant_id", "scope_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_order_acceptance_lines_tenant_id_paid_order_acceptance_snapshot_id_order_line_id",
                table: "ie_paid_order_acceptance_lines",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_order_acceptance_snapshots_tenant_id_event_id_accepted_at",
                table: "ie_paid_order_acceptance_snapshots",
                columns: new[] { "tenant_id", "event_id", "accepted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_order_acceptance_snapshots_tenant_id_registration_order_id_disclosure_revision",
                table: "ie_paid_order_acceptance_snapshots",
                columns: new[] { "tenant_id", "registration_order_id", "disclosure_revision" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ie_payment_attempts_ie_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_payment_attempts",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" },
                principalTable: "ie_paid_order_acceptance_snapshots",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ie_payment_attempts_ie_paid_order_acceptance_snapshots_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_payment_attempts");

            migrationBuilder.DropTable(
                name: "ie_paid_checkout_review_approvals");

            migrationBuilder.DropTable(
                name: "ie_paid_checkout_sale_control_audits");

            migrationBuilder.DropTable(
                name: "ie_paid_order_acceptance_lines");

            migrationBuilder.DropTable(
                name: "ie_paid_checkout_sale_controls");

            migrationBuilder.DropTable(
                name: "ie_paid_order_acceptance_snapshots");

            migrationBuilder.DropIndex(
                name: "ix_payment_reconciliation_effects_expired_lease_poll",
                table: "ie_payment_reconciliation_effects");

            migrationBuilder.DropIndex(
                name: "ix_payment_reconciliation_effects_worker_poll",
                table: "ie_payment_reconciliation_effects");

            migrationBuilder.DropIndex(
                name: "ix_ie_payment_attempts_tenant_id_paid_order_acceptance_snapshot_id",
                table: "ie_payment_attempts");

            migrationBuilder.DropColumn(
                name: "paid_order_acceptance_snapshot_id",
                table: "ie_payment_attempts");

            migrationBuilder.DropColumn(
                name: "per_event_sales_count_ceiling",
                table: "ie_paid_event_policy_currency_risk_limits");

            migrationBuilder.DropColumn(
                name: "rolling_organizer_sales_count_ceiling",
                table: "ie_paid_event_policy_currency_risk_limits");

            migrationBuilder.DropColumn(
                name: "rolling_organizer_window_days",
                table: "ie_paid_event_policy_currency_risk_limits");

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_reconciliation_effects_status_next_attempt_at_created_at",
                table: "ie_payment_reconciliation_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });
        }
    }
}
