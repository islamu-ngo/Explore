using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationPaymentAttemptDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_payment_attempt_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_payment_attempt_statuses", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_payment_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recipient_tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recipient_organizer_actor_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recipient_connection_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recipient_provider_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipient_connect_platform_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipient_external_account_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipient_merchant_country_code = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipient_currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipient_profile_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipient_instance_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recipient_tenant_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    recipient_snapshotted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    provider_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    profile_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_api_revision = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    composition_revision = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    total_minor = table.Column<long>(type: "bigint", nullable: false),
                    provider_idempotency_key = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active_scope_key = table.Column<string>(type: "varchar(170)", maxLength: 170, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active_uniqueness_slot = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_checkout_session_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_payment_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payment_attempt_status_id = table.Column<int>(type: "int", nullable: false),
                    authoritative_status_floor_id = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_status_observed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_provider_request_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dispatch_pending_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    requires_action_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    processing_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    succeeded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    unknown_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("pk_ie_payment_attempts", x => x.id);
                    table.UniqueConstraint("ak_payment_attempts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_payment_attempts_active_slot", "(payment_attempt_status_id IN (6, 7) AND active_uniqueness_slot <> 'active') OR active_uniqueness_slot = 'active'");
                    table.CheckConstraint("ck_payment_attempts_amounts", "organizer_amount_minor >= 0 AND platform_fee_minor >= 0 AND platform_contribution_minor >= 0 AND total_minor >= 0 AND platform_fee_minor <= organizer_amount_minor");
                    table.CheckConstraint("ck_payment_attempts_authoritative_status_floor", "authoritative_status_floor_id BETWEEN 1 AND 8");
                    table.CheckConstraint("ck_payment_attempts_status", "payment_attempt_status_id BETWEEN 1 AND 8");
                    table.ForeignKey(
                        name: "FK_ie_payment_attempts_ie_payment_attempt_statuses_auth_149D7CFD",
                        column: x => x.authoritative_status_floor_id,
                        principalTable: "ie_payment_attempt_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_payment_attempts_ie_payment_attempt_statuses_paym_D1D12C99",
                        column: x => x.payment_attempt_status_id,
                        principalTable: "ie_payment_attempt_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_payment_attempts_ie_registration_orders_tenant_id_94EC9F76",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_payment_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_checkout_dispatch_effects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    payment_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    status = table.Column<int>(type: "int", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    processing_lease_owner = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    processing_lease_token = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    processing_lease_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    next_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    parked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    unknown_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_failure_code = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_checkout_dispatch_effects", x => x.id);
                    table.UniqueConstraint("ak_checkout_dispatch_effects_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_checkout_dispatch_effects_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_checkout_dispatch_effects_processing_fence", "processing_fence >= 0");
                    table.CheckConstraint("ck_checkout_dispatch_effects_state", "(status IN (1, 4) AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NULL AND parked_at IS NULL) OR (status = 2 AND processing_lease_owner IS NOT NULL AND processing_lease_token IS NOT NULL AND processing_lease_expires_at IS NOT NULL AND completed_at IS NULL AND parked_at IS NULL) OR (status = 3 AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NOT NULL AND parked_at IS NULL) OR (status = 5 AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NULL AND parked_at IS NOT NULL) OR (status = 6 AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NULL AND parked_at IS NULL AND unknown_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ie_checkout_dispatch_effects_ie_payment_attempts_ten_CC3192CC",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalTable: "ie_payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_checkout_dispatch_effects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_checkout_dispatch_effects_status_next_attempt_at__1D32FEF6",
                table: "ie_checkout_dispatch_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_checkout_dispatch_effects_tenant_id_payment_attempt_id",
                table: "ie_checkout_dispatch_effects",
                columns: new[] { "tenant_id", "payment_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_attempt_statuses_master_code",
                table: "ie_payment_attempt_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_attempts_active_scope_key_active_uniqueness_slot",
                table: "ie_payment_attempts",
                columns: new[] { "active_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_attempts_authoritative_status_floor_id",
                table: "ie_payment_attempts",
                column: "authoritative_status_floor_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_attempts_payment_attempt_status_id",
                table: "ie_payment_attempts",
                column: "payment_attempt_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_payment_attempts_provider_idempotency_key",
                table: "ie_payment_attempts",
                column: "provider_idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_payment_attempts_tenant_id_registration_order_id__A4ABFDE9",
                table: "ie_payment_attempts",
                columns: new[] { "tenant_id", "registration_order_id", "payment_attempt_status_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_checkout_dispatch_effects");

            migrationBuilder.DropTable(
                name: "ie_payment_attempts");

            migrationBuilder.DropTable(
                name: "ie_payment_attempt_statuses");
        }
    }
}
