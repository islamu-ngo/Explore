using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidEventPaymentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_organizer_payment_provider_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    organizer_actor_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    provider_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    connect_platform_id = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    external_account_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active_scope_key = table.Column<string>(type: "varchar(232)", maxLength: 232, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active_uniqueness_slot = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    merchant_country_code = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    charge_capability_state_id = table.Column<int>(type: "int", nullable: false),
                    requirements_state_id = table.Column<int>(type: "int", nullable: false),
                    last_readiness_observed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_readiness_evidence_revision = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    replaces_connection_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    replaced_by_connection_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    replaced_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    disabled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    disabled_reason_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_organizer_payment_provider_connections", x => x.id);
                    table.UniqueConstraint("ak_organizer_payment_provider_connections_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_organizer_payment_provider_connections_charge_capability", "charge_capability_state_id BETWEEN 0 AND 3");
                    table.CheckConstraint("ck_organizer_payment_provider_connections_requirements", "requirements_state_id BETWEEN 0 AND 4");
                    table.CheckConstraint("ck_organizer_payment_provider_connections_status", "status_id BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_connections_ie_actors__0F3CD107",
                        column: x => x.organizer_actor_id,
                        principalTable: "ie_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_connections_ie_organiz_46A5BA55",
                        columns: x => new { x.tenant_id, x.replaces_connection_id },
                        principalTable: "ie_organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_connections_ie_organiz_A196D414",
                        columns: x => new { x.tenant_id, x.replaced_by_connection_id },
                        principalTable: "ie_organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_connections_ie_tenants_192CD8BA",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_event_policy_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    policy_scope_key = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    active_uniqueness_slot = table.Column<int>(type: "int", nullable: false),
                    is_payments_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    requires_local_verification = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    default_currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requires_first_paid_event_review = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    far_future_review_threshold_days = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_paid_event_policy_versions", x => x.id);
                    table.UniqueConstraint("ak_paid_event_policy_versions_policy_scope_key_id", x => new { x.policy_scope_key, x.id });
                    table.ForeignKey(
                        name: "fk_ie_paid_event_policy_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_organizer_payment_provider_connection_supported_currencies",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    organizer_payment_provider_connection_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_organizer_payment_provider_connection_supported_c_EA427E2B", x => new { x.tenant_id, x.organizer_payment_provider_connection_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_ie_organizer_payment_provider_connection_supported_c_52F01A27",
                        columns: x => new { x.tenant_id, x.organizer_payment_provider_connection_id },
                        principalTable: "ie_organizer_payment_provider_connections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_event_policy_allowed_currencies",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paid_event_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_paid_event_policy_allowed_currencies_policy_scope_157B3265", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_ie_paid_event_policy_allowed_currencies_ie_paid_even_6500671A",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalTable: "ie_paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_event_policy_allowed_organizer_kinds",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paid_event_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    actor_type_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_paid_event_policy_allowed_organizer_kinds_policy__C0ECB89C", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_ie_paid_event_policy_allowed_organizer_kinds_ie_paid_5CCAAC96",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalTable: "ie_paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_event_policy_currency_risk_limits",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paid_event_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    currency_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    per_event_sales_ceiling_minor = table.Column<long>(type: "bigint", nullable: true),
                    rolling_organizer_sales_ceiling_minor = table.Column<long>(type: "bigint", nullable: true),
                    high_value_review_threshold_minor = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_paid_event_policy_currency_risk_limits_policy_sco_49C5B414", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_ie_paid_event_policy_currency_risk_limits_ie_paid_ev_FEA4DB72",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalTable: "ie_paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_paid_event_policy_refund_protections",
                columns: table => new
                {
                    policy_scope_key = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    paid_event_policy_version_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ordinal = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    refund_protection_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ie_paid_event_policy_refund_protections_policy_scope_E975D842", x => new { x.policy_scope_key, x.paid_event_policy_version_id, x.ordinal });
                    table.ForeignKey(
                        name: "FK_ie_paid_event_policy_refund_protections_ie_paid_even_C4E3D6C2",
                        columns: x => new { x.policy_scope_key, x.paid_event_policy_version_id },
                        principalTable: "ie_paid_event_policy_versions",
                        principalColumns: new[] { "policy_scope_key", "id" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_connection_supported_c_281D0FF0",
                table: "ie_organizer_payment_provider_connection_supported_currencies",
                columns: new[] { "tenant_id", "organizer_payment_provider_connection_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_connections_active_sco_FC47D6BC",
                table: "ie_organizer_payment_provider_connections",
                columns: new[] { "active_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_organizer_payment_provider_connections_organizer_actor_id",
                table: "ie_organizer_payment_provider_connections",
                column: "organizer_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_connections_provider_c_666696AF",
                table: "ie_organizer_payment_provider_connections",
                columns: new[] { "provider_code", "connect_platform_id", "external_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_connections_tenant_id__354BD75C",
                table: "ie_organizer_payment_provider_connections",
                columns: new[] { "tenant_id", "organizer_actor_id", "provider_code", "connect_platform_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_connections_tenant_id__A176AF4F",
                table: "ie_organizer_payment_provider_connections",
                columns: new[] { "tenant_id", "replaces_connection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_organizer_payment_provider_connections_tenant_id__DAFAB243",
                table: "ie_organizer_payment_provider_connections",
                columns: new[] { "tenant_id", "replaced_by_connection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_event_policy_allowed_currencies_policy_scope_0D333DA9",
                table: "ie_paid_event_policy_allowed_currencies",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_event_policy_allowed_organizer_kinds_policy__44AC0DC2",
                table: "ie_paid_event_policy_allowed_organizer_kinds",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "actor_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_event_policy_currency_risk_limits_policy_sco_E6AEDB5C",
                table: "ie_paid_event_policy_currency_risk_limits",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_event_policy_refund_protections_policy_scope_65CD5877",
                table: "ie_paid_event_policy_refund_protections",
                columns: new[] { "policy_scope_key", "paid_event_policy_version_id", "refund_protection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_paid_event_policy_versions_policy_scope_key_activ_2B8F83EC",
                table: "ie_paid_event_policy_versions",
                columns: new[] { "policy_scope_key", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_event_policy_versions_policy_scope_key_version_number",
                table: "ie_paid_event_policy_versions",
                columns: new[] { "policy_scope_key", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_paid_event_policy_versions_tenant_id",
                table: "ie_paid_event_policy_versions",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_organizer_payment_provider_connection_supported_currencies");

            migrationBuilder.DropTable(
                name: "ie_paid_event_policy_allowed_currencies");

            migrationBuilder.DropTable(
                name: "ie_paid_event_policy_allowed_organizer_kinds");

            migrationBuilder.DropTable(
                name: "ie_paid_event_policy_currency_risk_limits");

            migrationBuilder.DropTable(
                name: "ie_paid_event_policy_refund_protections");

            migrationBuilder.DropTable(
                name: "ie_organizer_payment_provider_connections");

            migrationBuilder.DropTable(
                name: "ie_paid_event_policy_versions");
        }
    }
}
