using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class CompletePaidCheckoutSafetyActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ie_payment_reconciliation_effects_status_next_attemp_0A9DBC13",
                table: "ie_payment_reconciliation_effects");

            migrationBuilder.AddColumn<Guid>(
                name: "paid_order_acceptance_snapshot_id",
                table: "ie_payment_attempts",
                type: "char(36)",
                nullable: true)
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "per_event_sales_count_ceiling",
                table: "ie_paid_event_policy_currency_risk_limits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rolling_organizer_sales_count_ceiling",
                table: "ie_paid_event_policy_currency_risk_limits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rolling_organizer_window_days",
                table: "ie_paid_event_policy_currency_risk_limits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ie_paid_checkout_review_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    organizer_actor_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    paid_event_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    trigger_id = table.Column<int>(type: "int", nullable: false),
                    maximum_order_amount_minor = table.Column<long>(type: "bigint", nullable: true),
                    status_code = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    request_reason_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requested_by_user_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    review_reason_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
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
                        name: "FK_ie_paid_checkout_review_approvals_ie_events_tenant_i_7E9DCA26",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_checkout_sale_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scope_key = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_stopped = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    resume_requested_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    resume_requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    resume_reviewed_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    resume_reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_order_acceptance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    composition_revision = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    disclosure_revision = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    merchant_disclosure_text = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operator_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    operator_display_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_official_instance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    official_origin = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operator_region_code = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operator_website_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operator_legal_notice_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operator_terms_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    operator_privacy_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    complaint_contact = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    complaint_owner = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_owner = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dispute_owner = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reconciliation_owner = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activation_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    delivery_starts_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    delivery_ends_at_utc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    event_time_zone_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    total_minor = table.Column<long>(type: "bigint", nullable: false),
                    instance_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    refund_policy_version = table.Column<int>(type: "int", nullable: false),
                    refund_policy_text = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_policy_language_tag = table.Column<string>(type: "varchar(35)", maxLength: 35, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    support_contact = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_profile_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    charge_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    statement_descriptor = table.Column<string>(type: "varchar(22)", maxLength: 22, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_environment = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_credential_owner = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accepted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_paid_order_acceptance_snapshots", x => x.id);
                    table.UniqueConstraint("ak_paid_order_acceptance_snapshots_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_paid_order_acceptance_amounts", "organizer_amount_minor > 0 AND platform_fee_minor >= 0 AND platform_fee_minor <= organizer_amount_minor AND platform_contribution_minor >= 0 AND total_minor = organizer_amount_minor + platform_contribution_minor");
                    table.CheckConstraint("ck_paid_order_acceptance_delivery", "delivery_ends_at_utc > delivery_starts_at_utc");
                    table.CheckConstraint("ck_paid_order_acceptance_refund_version", "refund_policy_version > 0");
                    table.ForeignKey(
                        name: "FK_ie_paid_order_acceptance_snapshots_ie_events_tenant__E7920657",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_paid_order_acceptance_snapshots_ie_registration_o_4FE3C00B",
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_checkout_sale_control_audits",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    paid_checkout_sale_control_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    action_code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actor_user_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    subject_user_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_paid_checkout_sale_control_audits_tenant_id_paid__E95351BF", x => new { x.tenant_id, x.paid_checkout_sale_control_id, x.sequence });
                    table.CheckConstraint("ck_paid_checkout_sale_control_audits_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "FK_ie_paid_checkout_sale_control_audits_ie_paid_checkou_CCB2AC63",
                        columns: x => new { x.tenant_id, x.paid_checkout_sale_control_id },
                        principalTable: "ie_paid_checkout_sale_controls",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_order_acceptance_lines",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    order_line_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    discount_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    line_total_minor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_paid_order_acceptance_lines_tenant_id_paid_order__6D2D14AC", x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id, x.ordinal });
                    table.CheckConstraint("ck_paid_order_acceptance_lines_shape", "ordinal >= 0 AND quantity > 0 AND unit_amount_minor >= 0 AND discount_amount_minor >= 0 AND line_total_minor >= 0 AND discount_amount_minor <= unit_amount_minor * quantity AND line_total_minor = unit_amount_minor * quantity - discount_amount_minor");
                    table.ForeignKey(
                        name: "FK_ie_paid_order_acceptance_lines_ie_paid_order_accepta_2A9B114F",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id },
                        principalTable: "ie_paid_order_acceptance_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_payment_reconciliation_effects_status_next_attemp_E73E03AD",
                table: "ie_payment_reconciliation_effects",
                columns: new[] { "status", "next_attempt_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_payment_reconciliation_effects_status_processing__801F2D1D",
                table: "ie_payment_reconciliation_effects",
                columns: new[] { "status", "processing_lease_expires_at", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_payment_attempts_tenant_id_paid_order_acceptance__71D65AAB",
                table: "ie_payment_attempts",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_checkout_review_approvals_tenant_id_event_id_C1CD28B8",
                table: "ie_paid_checkout_review_approvals",
                columns: new[] { "tenant_id", "event_id", "organizer_actor_id", "paid_event_policy_version_id", "currency_code", "trigger_id", "status_code" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_checkout_sale_control_audits_tenant_id_event_99E39CA3",
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
                name: "IX_ie_paid_order_acceptance_lines_tenant_id_paid_order__953B0C90",
                table: "ie_paid_order_acceptance_lines",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_order_acceptance_snapshots_tenant_id_event_i_7920B1D6",
                table: "ie_paid_order_acceptance_snapshots",
                columns: new[] { "tenant_id", "event_id", "accepted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_order_acceptance_snapshots_tenant_id_registr_7FC12C43",
                table: "ie_paid_order_acceptance_snapshots",
                columns: new[] { "tenant_id", "registration_order_id", "disclosure_revision" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ie_payment_attempts_ie_paid_order_acceptance_snapsho_54440CA3",
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
                name: "FK_ie_payment_attempts_ie_paid_order_acceptance_snapsho_54440CA3",
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
                name: "IX_ie_payment_reconciliation_effects_status_next_attemp_E73E03AD",
                table: "ie_payment_reconciliation_effects");

            migrationBuilder.DropIndex(
                name: "IX_ie_payment_reconciliation_effects_status_processing__801F2D1D",
                table: "ie_payment_reconciliation_effects");

            migrationBuilder.DropIndex(
                name: "IX_ie_payment_attempts_tenant_id_paid_order_acceptance__71D65AAB",
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
                name: "IX_ie_payment_reconciliation_effects_status_next_attemp_0A9DBC13",
                table: "ie_payment_reconciliation_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });
        }
    }
}
