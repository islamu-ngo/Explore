using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidEventPaymentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizer_payment_provider_connections",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organizer_actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    connect_platform_id = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    external_account_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    active_scope_key = table.Column<string>(type: "nvarchar(232)", maxLength: 232, nullable: false),
                    active_uniqueness_slot = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    merchant_country_code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    charge_capability_state_id = table.Column<int>(type: "int", nullable: false),
                    requirements_state_id = table.Column<int>(type: "int", nullable: false),
                    last_readiness_observed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_readiness_evidence_revision = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    replaces_connection_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    replaced_by_connection_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    replaced_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    disabled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    disabled_reason_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizer_payment_provider_connections", x => x.id);
                    table.UniqueConstraint("ak_organizer_payment_provider_connections_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_organizer_payment_provider_connections_charge_capability", "charge_capability_state_id BETWEEN 0 AND 3");
                    table.CheckConstraint("ck_organizer_payment_provider_connections_requirements", "requirements_state_id BETWEEN 0 AND 4");
                    table.CheckConstraint("ck_organizer_payment_provider_connections_status", "status_id BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_organizer_payment_connections_replaced_by",
                        columns: x => new { x.tenant_id, x.replaced_by_connection_id },
                        principalSchema: "islamu_event",
                        principalTable: "organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizer_payment_connections_replaces",
                        columns: x => new { x.tenant_id, x.replaces_connection_id },
                        principalSchema: "islamu_event",
                        principalTable: "organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizer_payment_provider_connections_actors_organizer_actor_id",
                        column: x => x.organizer_actor_id,
                        principalSchema: "islamu_event",
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizer_payment_provider_connections_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paid_event_policy_versions",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    policy_scope_key = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    active_uniqueness_slot = table.Column<int>(type: "int", nullable: false),
                    is_payments_enabled = table.Column<bool>(type: "bit", nullable: false),
                    requires_local_verification = table.Column<bool>(type: "bit", nullable: false),
                    default_currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    requires_first_paid_event_review = table.Column<bool>(type: "bit", nullable: false),
                    far_future_review_threshold_days = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_event_policy_versions", x => x.id);
                    table.UniqueConstraint("ak_paid_event_policy_versions_policy_scope_key_id", x => new { x.policy_scope_key, x.id });
                    table.ForeignKey(
                        name: "fk_paid_event_policy_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organizer_payment_provider_connection_supported_currencies",
                schema: "islamu_event",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organizer_payment_provider_connection_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizer_payment_provider_connection_supported_currencies", x => new { x.tenant_id, x.organizer_payment_provider_connection_id, x.ordinal });
                    table.ForeignKey(
                        name: "fk_organizer_payment_provider_connection_supported_currencies_organizer_payment_provider_connections_tenant_id_organizer_paymen",
                        columns: x => new { x.tenant_id, x.organizer_payment_provider_connection_id },
                        principalSchema: "islamu_event",
                        principalTable: "organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" });
                });

            migrationBuilder.CreateTable(
                name: "paid_event_policy_allowed_currencies",
                schema: "islamu_event",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    paid_event_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_event_policy_allowed_currencies", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "fk_paid_event_policy_allowed_currencies_paid_event_policy_versions_policy_scope_key_paid_event_policy_version_id",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" });
                });

            migrationBuilder.CreateTable(
                name: "paid_event_policy_allowed_organizer_kinds",
                schema: "islamu_event",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    paid_event_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    actor_type_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_event_policy_allowed_organizer_kinds", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "fk_paid_event_policy_allowed_organizer_kinds_paid_event_policy_versions_policy_scope_key_paid_event_policy_version_id",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" });
                });

            migrationBuilder.CreateTable(
                name: "paid_event_policy_currency_risk_limits",
                schema: "islamu_event",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    paid_event_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    per_event_sales_ceiling_minor = table.Column<long>(type: "bigint", nullable: true),
                    rolling_organizer_sales_ceiling_minor = table.Column<long>(type: "bigint", nullable: true),
                    high_value_review_threshold_minor = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_event_policy_currency_risk_limits", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "fk_paid_event_policy_currency_risk_limits_paid_event_policy_versions_policy_scope_key_paid_event_policy_version_id",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" });
                });

            migrationBuilder.CreateTable(
                name: "paid_event_policy_refund_protections",
                schema: "islamu_event",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    paid_event_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    refund_protection_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paid_event_policy_refund_protections", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "fk_paid_event_policy_refund_protections_paid_event_policy_versions_policy_scope_key_paid_event_policy_version_id",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalSchema: "islamu_event",
                        principalTable: "paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" });
                });

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_connection_supported_currencies_tenant_id_organizer_payment_provider_connection_id_currency_code",
                schema: "islamu_event",
                table: "organizer_payment_provider_connection_supported_currencies",
                columns: new[] { "tenant_id", "organizer_payment_provider_connection_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_connections_active_scope_key_active_uniqueness_slot",
                schema: "islamu_event",
                table: "organizer_payment_provider_connections",
                columns: new[] { "active_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_connections_organizer_actor_id",
                schema: "islamu_event",
                table: "organizer_payment_provider_connections",
                column: "organizer_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_connections_provider_code_connect_platform_id_external_account_id",
                schema: "islamu_event",
                table: "organizer_payment_provider_connections",
                columns: new[] { "provider_code", "connect_platform_id", "external_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_connections_tenant_id_organizer_actor_id_provider_code_connect_platform_id_status_id",
                schema: "islamu_event",
                table: "organizer_payment_provider_connections",
                columns: new[] { "tenant_id", "organizer_actor_id", "provider_code", "connect_platform_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_connections_tenant_id_replaced_by_connection_id",
                schema: "islamu_event",
                table: "organizer_payment_provider_connections",
                columns: new[] { "tenant_id", "replaced_by_connection_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizer_payment_provider_connections_tenant_id_replaces_connection_id",
                schema: "islamu_event",
                table: "organizer_payment_provider_connections",
                columns: new[] { "tenant_id", "replaces_connection_id" });

            migrationBuilder.CreateIndex(
                name: "ix_paid_event_policy_allowed_currencies_policy_scope_key_paid_event_policy_version_id_currency_code",
                schema: "islamu_event",
                table: "paid_event_policy_allowed_currencies",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_event_policy_allowed_organizer_kinds_policy_scope_key_paid_event_policy_version_id_actor_type_id",
                schema: "islamu_event",
                table: "paid_event_policy_allowed_organizer_kinds",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "actor_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_event_policy_currency_risk_limits_policy_scope_key_paid_event_policy_version_id_currency_code",
                schema: "islamu_event",
                table: "paid_event_policy_currency_risk_limits",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_event_policy_refund_protections_policy_scope_key_paid_event_policy_version_id_refund_protection_id",
                schema: "islamu_event",
                table: "paid_event_policy_refund_protections",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "refund_protection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_event_policy_versions_policy_scope_key_active_uniqueness_slot",
                schema: "islamu_event",
                table: "paid_event_policy_versions",
                columns: new[] { "policy_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_event_policy_versions_policy_scope_key_version_number",
                schema: "islamu_event",
                table: "paid_event_policy_versions",
                columns: new[] { "policy_scope_key", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paid_event_policy_versions_tenant_id",
                schema: "islamu_event",
                table: "paid_event_policy_versions",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizer_payment_provider_connection_supported_currencies",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_event_policy_allowed_currencies",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_event_policy_allowed_organizer_kinds",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_event_policy_currency_risk_limits",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_event_policy_refund_protections",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "organizer_payment_provider_connections",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "paid_event_policy_versions",
                schema: "islamu_event");
        }
    }
}
