using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionCheckInAndLocationAddressGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ticket_type_entitlements_tenant_id_ticket_type_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements");

            migrationBuilder.AddColumn<string>(
                name: "display_sort_key",
                schema: "islamu_event",
                table: "locations",
                type: "character varying(14000)",
                maxLength: 14000,
                nullable: false,
                defaultValue: "",
                collation: "C");

            migrationBuilder.AddColumn<short>(
                name: "display_sort_key_version",
                schema: "islamu_event",
                table: "locations",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "address_substring_key",
                schema: "islamu_event",
                table: "location_pii",
                type: "character varying(14000)",
                maxLength: 14000,
                nullable: false,
                defaultValue: "",
                collation: "C");

            migrationBuilder.AddColumn<short>(
                name: "address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements",
                type: "uuid",
                nullable: false,
                computedColumnSql: "COALESCE(event_session_id, event_day_id, target_event_id)",
                stored: true);

            migrationBuilder.CreateTable(
                name: "admission_targets",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_type_id = table.Column<int>(type: "integer", nullable: false),
                    admission_operational_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_day_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_targets", x => x.id);
                    table.UniqueConstraint("ak_admission_targets_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_admission_targets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_targets_operational_status", "admission_operational_status_id IN (1, 2)");
                    table.CheckConstraint("ck_admission_targets_scope_shape", "(admission_target_type_id = 1 AND event_day_id IS NULL AND event_session_id IS NULL AND scope_id = event_id) OR (admission_target_type_id = 2 AND event_day_id IS NOT NULL AND event_session_id IS NULL AND scope_id = event_day_id) OR (admission_target_type_id = 3 AND event_day_id IS NULL AND event_session_id IS NOT NULL AND scope_id = event_session_id)");
                    table.ForeignKey(
                        name: "fk_admission_targets_event_days_tenant_id_event_id_event_day_id",
                        columns: x => new { x.tenant_id, x.event_id, x.event_day_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_targets_event_sessions_tenant_id_event_id_event_s",
                        columns: x => new { x.tenant_id, x.event_id, x.event_session_id },
                        principalSchema: "islamu_event",
                        principalTable: "event_sessions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_targets_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_targets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "location_address_sources",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_address_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "location_address_visibilities",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_address_visibilities", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "islamu_event",
                table: "location_address_sources",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "UNKNOWN_LEGACY", "Unknown legacy", "Address provenance predates explicit governance or is unknown" },
                    { 2, "MANUAL", "Manual", "Address was entered locally without a provider selection" },
                    { 3, "PROVIDER_SELECTION", "Provider selection", "Address originated from a protected provider selection" }
                });

            migrationBuilder.InsertData(
                schema: "islamu_event",
                table: "location_address_visibilities",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "QUARANTINED", "Quarantined", "Address is unavailable for local suggestion reuse" },
                    { 2, "CREATOR_PRIVATE", "Creator private", "Address reuse is limited to its creator" },
                    { 3, "ORGANIZATION_SCOPED", "Organization scoped", "Address reuse is limited to one tenant organization participation" },
                    { 4, "TENANT_APPROVED", "Tenant approved", "Address is approved for reuse across its tenant" }
                });

            migrationBuilder.AddColumn<Guid>(
                name: "address_organization_id",
                schema: "islamu_event",
                table: "locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "address_source_id",
                schema: "islamu_event",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "address_visibility_id",
                schema: "islamu_event",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "admission_check_in_policies",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opens_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closes_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    maximum_entries = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_check_in_policies", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_policies_maximum_entries", "maximum_entries > 0");
                    table.CheckConstraint("ck_admission_check_in_policies_window", "closes_at_utc > opens_at_utc");
                    table.ForeignKey(
                        name: "fk_admission_check_in_policies_admission_targets_tenant_id_adm",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_scanner_capabilities",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lookup_key_version = table.Column<int>(type: "integer", nullable: false),
                    lookup_digest = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    device_label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    actions = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    issued_by_actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_by_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_scanner_capabilities", x => x.id);
                    table.UniqueConstraint("ak_admission_scanner_capabilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_scanner_capabilities_expiry", "expires_at > issued_at");
                    table.CheckConstraint("ck_admission_scanner_capabilities_key_version", "lookup_key_version > 0");
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_actors_issued_by_actor_id",
                        column: x => x.issued_by_actor_id,
                        principalSchema: "islamu_event",
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_actors_revoked_by_actor_id",
                        column: x => x.revoked_by_actor_id,
                        principalSchema: "islamu_event",
                        principalTable: "actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_admission_targets_tenant_id_",
                        columns: x => new { x.tenant_id, x.event_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalSchema: "islamu_event",
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_scanner_capabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_check_in_events",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    admission_check_in_action_id = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scanner_capability_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admission_check_in_undo_reason_code_id = table.Column<int>(type: "integer", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    compensated_check_in_event_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_check_in_events", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_events_tenant_id_admission_ticket_id_adm", x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.id });
                    table.UniqueConstraint("ak_admission_check_in_events_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_events_action", "admission_check_in_action_id IN (1, 2)");
                    table.CheckConstraint("ck_admission_check_in_events_authority", "(actor_id IS NOT NULL AND scanner_capability_id IS NULL) OR (actor_id IS NULL AND scanner_capability_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_check_in_events_fact_shape", "(admission_check_in_action_id = 1 AND admission_check_in_undo_reason_code_id IS NULL AND compensated_check_in_event_id IS NULL) OR (admission_check_in_action_id = 2 AND admission_check_in_undo_reason_code_id IN (1, 2, 3, 4) AND compensated_check_in_event_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_check_in_events_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_check_in_events_tenant_",
                        columns: x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.compensated_check_in_event_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_check_in_events",
                        principalColumns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_scanner_capabilities_te",
                        columns: x => new { x.tenant_id, x.scanner_capability_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_scanner_capabilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_targets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_admission_tickets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admission_check_in_states",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active_check_in_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_count = table.Column<int>(type: "integer", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admission_check_in_states", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_states_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_states_counts", "entry_count >= 0 AND last_sequence >= 0");
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_admission_check_in_events_tenant_",
                        columns: x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.active_check_in_event_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_check_in_events",
                        principalColumns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_admission_targets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_admission_tickets_tenant_id_admis",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalSchema: "islamu_event",
                        principalTable: "admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admission_check_in_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ticket_type_entitlements_canonical_scope",
                schema: "islamu_event",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "ticket_type_id", "target_event_id", "entitlement_scope_type_id", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_locations_address_source_id",
                schema: "islamu_event",
                table: "locations",
                column: "address_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_address_visibility_id",
                schema: "islamu_event",
                table: "locations",
                column: "address_visibility_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_address_visibility_created_by",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_visibility_id", "created_by" });

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_address_visibility_organization",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_visibility_id", "address_organization_id" });

            migrationBuilder.CreateIndex(
                name: "ix_locations_tenant_id_address_organization_id",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_organization_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_address_visibility_scope",
                schema: "islamu_event",
                table: "locations",
                sql: "(address_visibility_id = 1 AND address_organization_id IS NULL) OR (address_visibility_id = 2 AND created_by IS NOT NULL AND address_organization_id IS NULL) OR (address_visibility_id = 3 AND created_by IS NOT NULL AND address_organization_id IS NOT NULL) OR address_visibility_id = 4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_display_sort_key_version",
                schema: "islamu_event",
                table: "locations",
                sql: "(display_sort_key_version = 0 AND display_sort_key = '') OR (display_sort_key_version = 1 AND display_sort_key <> '' AND length(display_sort_key) % 7 = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_erased_address_quarantined",
                schema: "islamu_event",
                table: "locations",
                sql: "location_privacy_state_id <> 3 OR (address_visibility_id = 1 AND address_organization_id IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_private_home_address_visibility",
                schema: "islamu_event",
                table: "locations",
                sql: "location_kind_id <> 5 OR address_visibility_id <> 4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_tenant_approved_display_sort_key",
                schema: "islamu_event",
                table: "locations",
                sql: "address_visibility_id <> 4 OR display_sort_key_version = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_location_pii_address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii",
                sql: "(address_substring_key_version = 0 AND address_substring_key = '') OR (address_substring_key_version = 1 AND address_substring_key <> '' AND length(address_substring_key) % 7 = 0)");

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_events_tenant_id_admission_target_id",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_events_tenant_id_admission_ticket_id_adm",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "compensated_check_in_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_events_tenant_id_scanner_capability_id",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "scanner_capability_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_check_in_events_sequence",
                schema: "islamu_event",
                table: "admission_check_in_events",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_check_in_policies_target",
                schema: "islamu_event",
                table: "admission_check_in_policies",
                columns: new[] { "tenant_id", "admission_target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_states_tenant_id_admission_target_id",
                schema: "islamu_event",
                table: "admission_check_in_states",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_check_in_states_tenant_id_admission_ticket_id_adm",
                schema: "islamu_event",
                table: "admission_check_in_states",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "active_check_in_event_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_check_in_states_ticket_target",
                schema: "islamu_event",
                table: "admission_check_in_states",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_issued_by_actor_id",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                column: "issued_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_revoked_by_actor_id",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                column: "revoked_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_target",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_scanner_capabilities_tenant_id_event_id_admission",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "event_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_scanner_capabilities_digest",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_scanner_capabilities_issue_request",
                schema: "islamu_event",
                table: "admission_scanner_capabilities",
                columns: new[] { "tenant_id", "issue_request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admission_targets_tenant_id_event_id_event_day_id",
                schema: "islamu_event",
                table: "admission_targets",
                columns: new[] { "tenant_id", "event_id", "event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_admission_targets_tenant_id_event_id_event_session_id",
                schema: "islamu_event",
                table: "admission_targets",
                columns: new[] { "tenant_id", "event_id", "event_session_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_targets_scope",
                schema: "islamu_event",
                table: "admission_targets",
                columns: new[] { "tenant_id", "event_id", "admission_target_type_id", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_location_address_sources_master_code",
                schema: "islamu_event",
                table: "location_address_sources",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_location_address_visibilities_master_code",
                schema: "islamu_event",
                table: "location_address_visibilities",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_location_address_sources_address_source_id",
                schema: "islamu_event",
                table: "locations",
                column: "address_source_id",
                principalSchema: "islamu_event",
                principalTable: "location_address_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_location_address_visibilities_address_visibility_",
                schema: "islamu_event",
                table: "locations",
                column: "address_visibility_id",
                principalSchema: "islamu_event",
                principalTable: "location_address_visibilities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_organization_tenants_tenant_id_address_organizati",
                schema: "islamu_event",
                table: "locations",
                columns: new[] { "tenant_id", "address_organization_id" },
                principalSchema: "islamu_event",
                principalTable: "organization_tenants",
                principalColumns: new[] { "tenant_id", "organization_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_locations_location_address_sources_address_source_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_location_address_visibilities_address_visibility_",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_organization_tenants_tenant_id_address_organizati",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropTable(
                name: "admission_check_in_policies",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_check_in_states",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "location_address_sources",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "location_address_visibilities",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_check_in_events",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_scanner_capabilities",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "admission_targets",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ux_ticket_type_entitlements_canonical_scope",
                schema: "islamu_event",
                table: "ticket_type_entitlements");

            migrationBuilder.DropIndex(
                name: "ix_locations_address_source_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_address_visibility_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_address_visibility_created_by",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_address_visibility_organization",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_tenant_id_address_organization_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_address_visibility_scope",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_display_sort_key_version",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_erased_address_quarantined",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_private_home_address_visibility",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_tenant_approved_display_sort_key",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_location_pii_address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii");

            migrationBuilder.DropColumn(
                name: "scope_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements");

            migrationBuilder.DropColumn(
                name: "address_organization_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "address_source_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "address_visibility_id",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "display_sort_key",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "display_sort_key_version",
                schema: "islamu_event",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "address_substring_key",
                schema: "islamu_event",
                table: "location_pii");

            migrationBuilder.DropColumn(
                name: "address_substring_key_version",
                schema: "islamu_event",
                table: "location_pii");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_type_entitlements_tenant_id_ticket_type_id",
                schema: "islamu_event",
                table: "ticket_type_entitlements",
                columns: new[] { "tenant_id", "ticket_type_id" });
        }
    }
}
