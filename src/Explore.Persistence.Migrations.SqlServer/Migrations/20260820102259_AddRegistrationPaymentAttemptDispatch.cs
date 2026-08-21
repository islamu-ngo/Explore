using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationPaymentAttemptDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_attempt_statuses",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    master_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_attempt_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipient_tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipient_organizer_actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipient_connection_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipient_provider_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    recipient_connect_platform_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    recipient_external_account_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    recipient_merchant_country_code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    recipient_currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    recipient_profile_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    recipient_instance_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipient_tenant_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    recipient_snapshotted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    provider_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    profile_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    provider_api_revision = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    composition_revision = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    organizer_amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_contribution_minor = table.Column<long>(type: "bigint", nullable: false),
                    total_minor = table.Column<long>(type: "bigint", nullable: false),
                    provider_idempotency_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    active_scope_key = table.Column<string>(type: "nvarchar(170)", maxLength: 170, nullable: false),
                    active_uniqueness_slot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    provider_checkout_session_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    provider_payment_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    payment_attempt_status_id = table.Column<int>(type: "int", nullable: false),
                    authoritative_status_floor_id = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_status_observed_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_provider_request_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    dispatch_pending_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    requires_action_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    processing_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    succeeded_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    failed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    unknown_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_attempts", x => x.id);
                    table.UniqueConstraint("ak_payment_attempts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_payment_attempts_active_slot", "(payment_attempt_status_id IN (6, 7) AND active_uniqueness_slot <> 'active') OR active_uniqueness_slot = 'active'");
                    table.CheckConstraint("ck_payment_attempts_amounts", "organizer_amount_minor >= 0 AND platform_fee_minor >= 0 AND platform_contribution_minor >= 0 AND total_minor >= 0 AND platform_fee_minor <= organizer_amount_minor");
                    table.CheckConstraint("ck_payment_attempts_authoritative_status_floor", "authoritative_status_floor_id BETWEEN 1 AND 8");
                    table.CheckConstraint("ck_payment_attempts_status", "payment_attempt_status_id BETWEEN 1 AND 8");
                    table.ForeignKey(
                        name: "fk_payment_attempts_payment_attempt_statuses_authoritative_status_floor_id",
                        column: x => x.authoritative_status_floor_id,
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempt_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_attempts_payment_attempt_statuses_payment_attempt_status_id",
                        column: x => x.payment_attempt_status_id,
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempt_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_attempts_registration_orders_tenant_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_attempts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checkout_dispatch_effects",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    processing_lease_owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processing_lease_expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    next_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    parked_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    unknown_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_failure_code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_dispatch_effects", x => x.id);
                    table.UniqueConstraint("ak_checkout_dispatch_effects_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_checkout_dispatch_effects_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_checkout_dispatch_effects_processing_fence", "processing_fence >= 0");
                    table.CheckConstraint("ck_checkout_dispatch_effects_state", "(status IN (1, 4) AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NULL AND parked_at IS NULL) OR (status = 2 AND processing_lease_owner IS NOT NULL AND processing_lease_token IS NOT NULL AND processing_lease_expires_at IS NOT NULL AND completed_at IS NULL AND parked_at IS NULL) OR (status = 3 AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NOT NULL AND parked_at IS NULL) OR (status = 5 AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NULL AND parked_at IS NOT NULL) OR (status = 6 AND processing_lease_owner IS NULL AND processing_lease_token IS NULL AND processing_lease_expires_at IS NULL AND completed_at IS NULL AND parked_at IS NULL AND unknown_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_checkout_dispatch_effects_payment_attempts_tenant_id_payment_attempt_id",
                        columns: x => new { x.tenant_id, x.payment_attempt_id },
                        principalSchema: "islamu_event",
                        principalTable: "payment_attempts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_checkout_dispatch_effects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_checkout_dispatch_effects_tenant_id_payment_attempt_id",
                schema: "islamu_event",
                table: "checkout_dispatch_effects",
                columns: new[] { "tenant_id", "payment_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_checkout_dispatch_effects_worker_poll",
                schema: "islamu_event",
                table: "checkout_dispatch_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempt_statuses_master_code",
                schema: "islamu_event",
                table: "payment_attempt_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_active_scope_key_active_uniqueness_slot",
                schema: "islamu_event",
                table: "payment_attempts",
                columns: new[] { "active_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_authoritative_status_floor_id",
                schema: "islamu_event",
                table: "payment_attempts",
                column: "authoritative_status_floor_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_payment_attempt_status_id",
                schema: "islamu_event",
                table: "payment_attempts",
                column: "payment_attempt_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_provider_idempotency_key",
                schema: "islamu_event",
                table: "payment_attempts",
                column: "provider_idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_tenant_id_registration_order_id_payment_attempt_status_id",
                schema: "islamu_event",
                table: "payment_attempts",
                columns: new[] { "tenant_id", "registration_order_id", "payment_attempt_status_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_dispatch_effects",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "payment_attempts",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "payment_attempt_statuses",
                schema: "islamu_event");
        }
    }
}
