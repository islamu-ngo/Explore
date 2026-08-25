using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationRefundProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "campaign_cursor",
                table: "ie_payment_attempts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ie_paid_order_acceptance_lines_tenant_id_paid_order__044A7C81",
                table: "ie_paid_order_acceptance_lines",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" });

            migrationBuilder.CreateTable(
                name: "ie_payment_disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    payment_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    provider_dispute_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stage = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_observed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    response_due_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_payment_disputes", x => x.id);
                    table.UniqueConstraint("ak_payment_disputes_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_payment_disputes_amount", "amount_minor > 0");
                    table.CheckConstraint("ck_payment_disputes_stage", "stage BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_payment_disputes_status", "status BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_ie_payment_disputes_ie_payment_attempts_tenant_id_pa_31371848",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalTable: "ie_payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_payment_disputes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_refund_campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    kind = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    decision_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    decision_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    cursor = table.Column<long>(type: "bigint", nullable: false),
                    processing_lease_token = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    processing_lease_owner = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    processing_lease_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    total_payment_count = table.Column<int>(type: "int", nullable: false),
                    generated_count = table.Column<int>(type: "int", nullable: false),
                    pending_count = table.Column<int>(type: "int", nullable: false),
                    succeeded_count = table.Column<int>(type: "int", nullable: false),
                    failed_count = table.Column<int>(type: "int", nullable: false),
                    unknown_count = table.Column<int>(type: "int", nullable: false),
                    operator_case_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_refund_campaigns", x => x.id);
                    table.UniqueConstraint("ak_refund_campaigns_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_refund_campaigns_counts", "total_payment_count >= 0 AND generated_count >= 0 AND pending_count >= 0 AND succeeded_count >= 0 AND failed_count >= 0 AND unknown_count >= 0 AND operator_case_count >= 0 AND generated_count <= total_payment_count");
                    table.CheckConstraint("ck_refund_campaigns_cursor", "cursor >= 0");
                    table.CheckConstraint("ck_refund_campaigns_kind", "kind BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_refund_campaigns_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_ie_refund_campaigns_ie_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_refund_campaigns_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_refund_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    payment_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    source_campaign_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    reservation_source_key = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authority_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_policy_version = table.Column<int>(type: "int", nullable: false),
                    refund_policy_text = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refund_policy_language_tag = table.Column<string>(type: "varchar(35)", maxLength: 35, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    external_account_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_payment_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_idempotency_key = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allocation_organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    allocation_platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    allocation_platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    allocation_total_minor = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    provider_refund_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_provider_request_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    failure_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    buyer_refund_succeeded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    application_fee_refunded_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    last_observed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    succeeded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_refund_attempts", x => x.id);
                    table.UniqueConstraint("ak_refund_attempts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_refund_attempts_allocation", "allocation_organizer_amount_minor >= 0 AND allocation_platform_fee_minor >= 0 AND allocation_platform_contribution_minor >= 0 AND allocation_total_minor > 0 AND allocation_platform_fee_minor <= allocation_organizer_amount_minor AND allocation_total_minor = allocation_organizer_amount_minor + allocation_platform_contribution_minor");
                    table.CheckConstraint("ck_refund_attempts_buyer_success_capacity", "buyer_refund_succeeded_at IS NULL OR status NOT IN (7, 8)");
                    table.CheckConstraint("ck_refund_attempts_fee_refund", "application_fee_refunded_amount_minor >= 0");
                    table.CheckConstraint("ck_refund_attempts_policy_version", "refund_policy_version > 0");
                    table.CheckConstraint("ck_refund_attempts_status", "status BETWEEN 1 AND 8");
                    table.ForeignKey(
                        name: "FK_ie_refund_attempts_ie_paid_order_acceptance_snapshot_F4910048",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id },
                        principalTable: "ie_paid_order_acceptance_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_refund_attempts_ie_payment_attempts_tenant_id_pay_9DAC73DD",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalTable: "ie_payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_refund_attempts_ie_refund_campaigns_tenant_id_sou_6C56D7C0",
                        columns: x => new { x.tenant_id, x.source_campaign_id },
                        principalTable: "ie_refund_campaigns",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_refund_attempts_ie_registration_orders_tenant_id__981CB49D",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_refund_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_registration_material_change_choices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    refund_campaign_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    payment_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    status = table.Column<int>(type: "int", nullable: false),
                    decided_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_material_change_choices", x => x.id);
                    table.UniqueConstraint("ak_registration_material_change_choices_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_registration_material_change_choices_ie_paid_orde_E3CB71EF",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id },
                        principalTable: "ie_paid_order_acceptance_snapshots",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_registration_material_change_choices_ie_payment_a_BC9D44B9",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalTable: "ie_payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_registration_material_change_choices_ie_refund_ca_296752E9",
                        columns: x => new { x.tenant_id, x.refund_campaign_id },
                        principalTable: "ie_refund_campaigns",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_refund_line_allocations",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    refund_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    order_line_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    paid_order_acceptance_snapshot_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    total_minor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_refund_line_allocations_tenant_id_refund_attempt__9CD7AFB6", x => new { x.tenant_id, x.refund_attempt_id, x.order_line_id });
                    table.CheckConstraint("ck_refund_line_allocations_money", "ordinal >= 0 AND organizer_amount_minor >= 0 AND platform_fee_minor >= 0 AND platform_contribution_minor >= 0 AND total_minor >= 0 AND platform_fee_minor <= organizer_amount_minor AND total_minor = organizer_amount_minor + platform_contribution_minor");
                    table.ForeignKey(
                        name: "FK_ie_refund_line_allocations_ie_paid_order_acceptance__C07FDE01",
                        columns: x => new { x.tenant_id, x.paid_order_acceptance_snapshot_id, x.order_line_id },
                        principalTable: "ie_paid_order_acceptance_lines",
                        principalColumns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_refund_line_allocations_ie_refund_attempts_tenant_F000C1F3",
                        columns: x => new { x.tenant_id, x.refund_attempt_id },
                        principalTable: "ie_refund_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_attempts_tenant_id_campaign_cursor",
                table: "ie_payment_attempts",
                columns: new[] { "tenant_id", "campaign_cursor" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_attempts_campaign_cursor",
                table: "ie_payment_attempts",
                sql: "campaign_cursor > 0");

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_disputes_tenant_id_payment_attempt_id_status",
                table: "ie_payment_disputes",
                columns: new[] { "tenant_id", "payment_attempt_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_disputes_tenant_id_provider_dispute_id",
                table: "ie_payment_disputes",
                columns: new[] { "tenant_id", "provider_dispute_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_refund_attempts_tenant_id_paid_order_acceptance_s_E347DFAD",
                table: "ie_refund_attempts",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_refund_attempts_tenant_id_payment_attempt_id_status",
                table: "ie_refund_attempts",
                columns: new[] { "tenant_id", "payment_attempt_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_refund_attempts_tenant_id_provider_code_external__6EA071AD",
                table: "ie_refund_attempts",
                columns: new[] { "tenant_id", "provider_code", "external_account_id", "provider_refund_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_refund_attempts_tenant_id_provider_idempotency_key",
                table: "ie_refund_attempts",
                columns: new[] { "tenant_id", "provider_idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_refund_attempts_tenant_id_registration_order_id",
                table: "ie_refund_attempts",
                columns: new[] { "tenant_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_refund_attempts_tenant_id_reservation_source_key__C1911B17",
                table: "ie_refund_attempts",
                columns: new[] { "tenant_id", "reservation_source_key", "payment_attempt_id", "paid_order_acceptance_snapshot_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_refund_attempts_tenant_id_source_campaign_id_status",
                table: "ie_refund_attempts",
                columns: new[] { "tenant_id", "source_campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_refund_campaigns_status_processing_lease_expires__4F06110A",
                table: "ie_refund_campaigns",
                columns: new[] { "status", "processing_lease_expires_at", "decision_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_refund_campaigns_tenant_id_event_id_kind_decision_at",
                table: "ie_refund_campaigns",
                columns: new[] { "tenant_id", "event_id", "kind", "decision_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_refund_line_allocations_tenant_id_paid_order_acce_DEBA17F3",
                table: "ie_refund_line_allocations",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id", "order_line_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_refund_line_allocations_tenant_id_refund_attempt__9B95FD00",
                table: "ie_refund_line_allocations",
                columns: new[] { "tenant_id", "refund_attempt_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_material_change_choices_tenant_id_pa_8D392565",
                table: "ie_registration_material_change_choices",
                columns: new[] { "tenant_id", "payment_attempt_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_material_change_choices_tenant_id_pa_EE515C89",
                table: "ie_registration_material_change_choices",
                columns: new[] { "tenant_id", "paid_order_acceptance_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_material_change_choices_tenant_id_re_26376CBB",
                table: "ie_registration_material_change_choices",
                columns: new[] { "tenant_id", "refund_campaign_id", "payment_attempt_id", "paid_order_acceptance_snapshot_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_material_change_choices_tenant_id_re_638BC2EB",
                table: "ie_registration_material_change_choices",
                columns: new[] { "tenant_id", "registration_order_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_payment_disputes");

            migrationBuilder.DropTable(
                name: "ie_refund_line_allocations");

            migrationBuilder.DropTable(
                name: "ie_registration_material_change_choices");

            migrationBuilder.DropTable(
                name: "ie_refund_attempts");

            migrationBuilder.DropTable(
                name: "ie_refund_campaigns");

            migrationBuilder.DropIndex(
                name: "ix_ie_payment_attempts_tenant_id_campaign_cursor",
                table: "ie_payment_attempts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_attempts_campaign_cursor",
                table: "ie_payment_attempts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ie_paid_order_acceptance_lines_tenant_id_paid_order__044A7C81",
                table: "ie_paid_order_acceptance_lines");

            migrationBuilder.DropColumn(
                name: "campaign_cursor",
                table: "ie_payment_attempts");
        }
    }
}
