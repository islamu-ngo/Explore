using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipationHandlingModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "external_registration_url",
                table: "events");

            migrationBuilder.DropColumn(
                name: "is_registration_required",
                table: "events");

            migrationBuilder.CreateTable(
                name: "advance_registration_obligations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_advance_registration_obligations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capacity_oversell_policies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_capacity_oversell_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entitlement_scope_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entitlement_scope_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entitlement_selection_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entitlement_selection_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_access_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_access_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participant_data_collection_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participant_data_collection_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participation_handling_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participation_handling_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_contribution_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    heading = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_contribution_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    fee_basis_points = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_fee_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_catalog_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_catalog_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_pricing_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_pricing_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_capacity_pools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    maximum_quantity = table.Column<int>(type: "integer", nullable: true),
                    hold_duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    capacity_oversell_policy_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_event_capacity_pools", x => x.id);
                    table.UniqueConstraint("ak_event_capacity_pools_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_capacity_pools_capacity_oversell_policies_capacity_ov",
                        column: x => x.capacity_oversell_policy_id,
                        principalTable: "capacity_oversell_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_capacity_pools_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_capacity_pools_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_participation_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participation_handling_mode_id = table.Column<int>(type: "integer", nullable: false),
                    advance_registration_obligation_id = table.Column<int>(type: "integer", nullable: false),
                    identity_access_mode_id = table.Column<int>(type: "integer", nullable: true),
                    guest_recovery_policy = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_participation_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_advance_registration_obl",
                        column: x => x.advance_registration_obligation_id,
                        principalTable: "advance_registration_obligations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_events_tenant_id_id",
                        columns: x => new { x.tenant_id, x.id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_identity_access_modes_id",
                        column: x => x.identity_access_mode_id,
                        principalTable: "identity_access_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_participation_handling_m",
                        column: x => x.participation_handling_mode_id,
                        principalTable: "participation_handling_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_participation_configurations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_contribution_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contribution_basis_points = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    platform_contribution_setting_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_contribution_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_contribution_options_platform_contribution_setting",
                        column: x => x.platform_contribution_setting_id,
                        principalTable: "platform_contribution_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_fixed_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    platform_fee_policy_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_fee_fixed_charges", x => x.id);
                    table.ForeignKey(
                        name: "fk_platform_fee_fixed_charges_platform_fee_policies_platform_f",
                        column: x => x.platform_fee_policy_id,
                        principalTable: "platform_fee_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_ticket_catalog_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    ticket_catalog_status_id = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_event_ticket_catalog_versions", x => x.id);
                    table.UniqueConstraint("ak_event_ticket_catalog_versions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_ticket_catalog_versions_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_ticket_catalog_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_ticket_catalog_versions_ticket_catalog_statuses_ticke",
                        column: x => x.ticket_catalog_status_id,
                        principalTable: "ticket_catalog_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_ticket_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ticket_pricing_mode_id = table.Column<int>(type: "integer", nullable: false),
                    fixed_price_minor = table.Column<long>(type: "bigint", nullable: true),
                    minimum_price_minor = table.Column<long>(type: "bigint", nullable: true),
                    suggested_price_minor = table.Column<long>(type: "bigint", nullable: true),
                    participant_data_collection_mode_id = table.Column<int>(type: "integer", nullable: false),
                    capacity_pool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    minimum_age = table.Column<int>(type: "integer", nullable: true),
                    maximum_age = table.Column<int>(type: "integer", nullable: true),
                    requires_guardian = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    per_order_limit = table.Column<int>(type: "integer", nullable: true),
                    per_account_limit = table.Column<int>(type: "integer", nullable: true),
                    per_verified_contact_limit = table.Column<int>(type: "integer", nullable: true),
                    per_booking_party_limit = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_event_ticket_types", x => x.id);
                    table.UniqueConstraint("ak_event_ticket_types_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_ticket_types_event_capacity_pools_tenant_id_capacity_",
                        columns: x => new { x.tenant_id, x.capacity_pool_id },
                        principalTable: "event_capacity_pools",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_ticket_types_event_ticket_catalog_versions_tenant_id_",
                        columns: x => new { x.tenant_id, x.catalog_id },
                        principalTable: "event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_ticket_types_participant_data_collection_modes_partic",
                        column: x => x.participant_data_collection_mode_id,
                        principalTable: "participant_data_collection_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_ticket_types_ticket_pricing_modes_ticket_pricing_mode",
                        column: x => x.ticket_pricing_mode_id,
                        principalTable: "ticket_pricing_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ticket_type_entitlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entitlement_scope_type_id = table.Column<int>(type: "integer", nullable: false),
                    event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    included_quantity = table.Column<int>(type: "integer", nullable: false),
                    entitlement_selection_rule_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_type_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_ticket_type_entitlements_entitlement_scope_types_entitlemen",
                        column: x => x.entitlement_scope_type_id,
                        principalTable: "entitlement_scope_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_type_entitlements_entitlement_selection_rules_entitl",
                        column: x => x.entitlement_selection_rule_id,
                        principalTable: "entitlement_selection_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_type_entitlements_event_days_tenant_id_target_event_",
                        columns: x => new { x.tenant_id, x.target_event_id, x.event_day_id },
                        principalTable: "event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_type_entitlements_event_sessions_tenant_id_target_ev",
                        columns: x => new { x.tenant_id, x.target_event_id, x.event_session_id },
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_type_entitlements_event_ticket_types_tenant_id_ticke",
                        columns: x => new { x.tenant_id, x.ticket_type_id },
                        principalTable: "event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_type_entitlements_events_tenant_id_target_event_id",
                        columns: x => new { x.tenant_id, x.target_event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_advance_registration_obligations_master_code",
                table: "advance_registration_obligations",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_capacity_oversell_policies_master_code",
                table: "capacity_oversell_policies",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_scope_types_master_code",
                table: "entitlement_scope_types",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entitlement_selection_rules_master_code",
                table: "entitlement_selection_rules",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_capacity_pools_capacity_oversell_policy_id",
                table: "event_capacity_pools",
                column: "capacity_oversell_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_capacity_pools_tenant_id_event_id_name",
                table: "event_capacity_pools",
                columns: new[] { "tenant_id", "event_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_advance_registration_obl",
                table: "event_participation_configurations",
                column: "advance_registration_obligation_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_identity_access_mode_id",
                table: "event_participation_configurations",
                column: "identity_access_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_participation_handling_m",
                table: "event_participation_configurations",
                column: "participation_handling_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_participation_configurations_tenant_id_id",
                table: "event_participation_configurations",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_ticket_catalog_versions_tenant_id_event_id",
                table: "event_ticket_catalog_versions",
                columns: new[] { "tenant_id", "event_id" },
                unique: true,
                filter: "ticket_catalog_status_id = 2 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_ticket_catalog_versions_tenant_id_event_id_version_nu",
                table: "event_ticket_catalog_versions",
                columns: new[] { "tenant_id", "event_id", "version_number" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_event_ticket_catalog_versions_ticket_catalog_status_id",
                table: "event_ticket_catalog_versions",
                column: "ticket_catalog_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_ticket_types_participant_data_collection_mode_id",
                table: "event_ticket_types",
                column: "participant_data_collection_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_ticket_types_tenant_id_capacity_pool_id",
                table: "event_ticket_types",
                columns: new[] { "tenant_id", "capacity_pool_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_ticket_types_tenant_id_catalog_id",
                table: "event_ticket_types",
                columns: new[] { "tenant_id", "catalog_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_ticket_types_ticket_pricing_mode_id",
                table: "event_ticket_types",
                column: "ticket_pricing_mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_modes_master_code",
                table: "identity_access_modes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participant_data_collection_modes_master_code",
                table: "participant_data_collection_modes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participation_handling_modes_master_code",
                table: "participation_handling_modes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_contribution_options_platform_contribution_setting",
                table: "platform_contribution_options",
                columns: new[] { "platform_contribution_setting_id", "contribution_basis_points" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_contribution_options_platform_contribution_setting1",
                table: "platform_contribution_options",
                columns: new[] { "platform_contribution_setting_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_contribution_settings_is_active",
                table: "platform_contribution_settings",
                column: "is_active",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_platform_contribution_settings_version_number",
                table: "platform_contribution_settings",
                column: "version_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_fee_fixed_charges_platform_fee_policy_id_currency_",
                table: "platform_fee_fixed_charges",
                columns: new[] { "platform_fee_policy_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_platform_fee_policies_is_active",
                table: "platform_fee_policies",
                column: "is_active",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_platform_fee_policies_version_number",
                table: "platform_fee_policies",
                column: "version_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_catalog_statuses_master_code",
                table: "ticket_catalog_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_pricing_modes_master_code",
                table: "ticket_pricing_modes",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_type_entitlements_entitlement_scope_type_id",
                table: "ticket_type_entitlements",
                column: "entitlement_scope_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_type_entitlements_entitlement_selection_rule_id",
                table: "ticket_type_entitlements",
                column: "entitlement_selection_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_type_entitlements_tenant_id_target_event_id_event_da",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "target_event_id", "event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_type_entitlements_tenant_id_target_event_id_event_se",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "target_event_id", "event_session_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_type_entitlements_tenant_id_ticket_type_id",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "ticket_type_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_participation_configurations");

            migrationBuilder.DropTable(
                name: "platform_contribution_options");

            migrationBuilder.DropTable(
                name: "platform_fee_fixed_charges");

            migrationBuilder.DropTable(
                name: "ticket_type_entitlements");

            migrationBuilder.DropTable(
                name: "advance_registration_obligations");

            migrationBuilder.DropTable(
                name: "identity_access_modes");

            migrationBuilder.DropTable(
                name: "participation_handling_modes");

            migrationBuilder.DropTable(
                name: "platform_contribution_settings");

            migrationBuilder.DropTable(
                name: "platform_fee_policies");

            migrationBuilder.DropTable(
                name: "entitlement_scope_types");

            migrationBuilder.DropTable(
                name: "entitlement_selection_rules");

            migrationBuilder.DropTable(
                name: "event_ticket_types");

            migrationBuilder.DropTable(
                name: "event_capacity_pools");

            migrationBuilder.DropTable(
                name: "event_ticket_catalog_versions");

            migrationBuilder.DropTable(
                name: "participant_data_collection_modes");

            migrationBuilder.DropTable(
                name: "ticket_pricing_modes");

            migrationBuilder.DropTable(
                name: "capacity_oversell_policies");

            migrationBuilder.DropTable(
                name: "ticket_catalog_statuses");

            migrationBuilder.AddColumn<string>(
                name: "external_registration_url",
                table: "events",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_registration_required",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
