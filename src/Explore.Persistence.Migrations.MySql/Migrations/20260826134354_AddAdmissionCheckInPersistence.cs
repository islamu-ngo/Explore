using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionCheckInPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ie_ticket_type_entitlements_tenant_id_ticket_type_id",
                table: "ie_ticket_type_entitlements");

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                table: "ie_ticket_type_entitlements",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "ie_admission_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_target_type_id = table.Column<int>(type: "int", nullable: false),
                    admission_operational_status_id = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    scope_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_day_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_session_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_targets", x => x.id);
                    table.UniqueConstraint("ak_admission_targets_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_admission_targets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_targets_operational_status", "admission_operational_status_id IN (1, 2)");
                    table.CheckConstraint("ck_admission_targets_scope_shape", "(admission_target_type_id = 1 AND event_day_id IS NULL AND event_session_id IS NULL AND scope_id = event_id) OR (admission_target_type_id = 2 AND event_day_id IS NOT NULL AND event_session_id IS NULL AND scope_id = event_day_id) OR (admission_target_type_id = 3 AND event_day_id IS NULL AND event_session_id IS NOT NULL AND scope_id = event_session_id)");
                    table.ForeignKey(
                        name: "FK_ie_admission_targets_ie_event_days_tenant_id_event_i_2DBFC04B",
                        columns: x => new { x.tenant_id, x.event_id, x.event_day_id },
                        principalTable: "ie_event_days",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_targets_ie_event_sessions_tenant_id_eve_980BD39B",
                        columns: x => new { x.tenant_id, x.event_id, x.event_session_id },
                        principalTable: "ie_event_sessions",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_targets_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_targets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_check_in_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_target_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    opens_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    closes_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    maximum_entries = table.Column<int>(type: "int", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_check_in_policies", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_policies_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_policies_maximum_entries", "maximum_entries > 0");
                    table.CheckConstraint("ck_admission_check_in_policies_window", "closes_at_utc > opens_at_utc");
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_policies_ie_admission_targets__763D314E",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalTable: "ie_admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_scanner_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    issue_request_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_target_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    lookup_key_version = table.Column<int>(type: "int", nullable: false),
                    lookup_digest = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_label = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actions = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    issued_by_actor_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    issued_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revoked_by_actor_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revocation_reason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_scanner_capabilities", x => x.id);
                    table.UniqueConstraint("ak_admission_scanner_capabilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_scanner_capabilities_expiry", "expires_at > issued_at");
                    table.CheckConstraint("ck_admission_scanner_capabilities_key_version", "lookup_key_version > 0");
                    table.ForeignKey(
                        name: "FK_ie_admission_scanner_capabilities_ie_actors_issued_b_D491BA1B",
                        column: x => x.issued_by_actor_id,
                        principalTable: "ie_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_scanner_capabilities_ie_actors_revoked__0FDC9CAE",
                        column: x => x.revoked_by_actor_id,
                        principalTable: "ie_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_scanner_capabilities_ie_admission_targe_7139C180",
                        columns: x => new { x.tenant_id, x.event_id, x.admission_target_id },
                        principalTable: "ie_admission_targets",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_scanner_capabilities_ie_events_tenant_i_92531AE1",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "ie_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_scanner_capabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_check_in_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_target_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    admission_check_in_action_id = table.Column<int>(type: "int", nullable: false),
                    actor_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    scanner_capability_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_check_in_undo_reason_code_id = table.Column<int>(type: "int", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    compensated_check_in_event_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_check_in_events", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_events_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.UniqueConstraint("AK_ie_admission_check_in_events_tenant_id_admission_tic_DAC22C4B", x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_events_action", "admission_check_in_action_id IN (1, 2)");
                    table.CheckConstraint("ck_admission_check_in_events_authority", "(actor_id IS NOT NULL AND scanner_capability_id IS NULL) OR (actor_id IS NULL AND scanner_capability_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_check_in_events_fact_shape", "(admission_check_in_action_id = 1 AND admission_check_in_undo_reason_code_id IS NULL AND compensated_check_in_event_id IS NULL) OR (admission_check_in_action_id = 2 AND admission_check_in_undo_reason_code_id IN (1, 2, 3, 4) AND compensated_check_in_event_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_check_in_events_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_events_ie_admission_check_in_e_98F36173",
                        columns: x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.compensated_check_in_event_id },
                        principalTable: "ie_admission_check_in_events",
                        principalColumns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_events_ie_admission_scanner_ca_95CD3CBB",
                        columns: x => new { x.tenant_id, x.scanner_capability_id },
                        principalTable: "ie_admission_scanner_capabilities",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_events_ie_admission_targets_te_75494AA9",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalTable: "ie_admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_events_ie_admission_tickets_te_68A74BE2",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_check_in_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_check_in_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_target_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    active_check_in_event_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    entry_count = table.Column<int>(type: "int", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_check_in_states", x => x.id);
                    table.UniqueConstraint("ak_admission_check_in_states_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_check_in_states_counts", "entry_count >= 0 AND last_sequence >= 0");
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_states_ie_admission_check_in_e_BD981186",
                        columns: x => new { x.tenant_id, x.admission_ticket_id, x.admission_target_id, x.active_check_in_event_id },
                        principalTable: "ie_admission_check_in_events",
                        principalColumns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_states_ie_admission_targets_te_980E1E5A",
                        columns: x => new { x.tenant_id, x.admission_target_id },
                        principalTable: "ie_admission_targets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_check_in_states_ie_admission_tickets_te_6EC11450",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_check_in_states_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_ticket_type_entitlements_tenant_id_ticket_type_id_47B23051",
                table: "ie_ticket_type_entitlements",
                columns: new[] { "tenant_id", "ticket_type_id", "target_event_id", "entitlement_scope_type_id", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_check_in_events_tenant_id_admission_target_id",
                table: "ie_admission_check_in_events",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_check_in_events_tenant_id_admission_tic_44BC5D1E",
                table: "ie_admission_check_in_events",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_check_in_events_tenant_id_admission_tic_DD4EA309",
                table: "ie_admission_check_in_events",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "compensated_check_in_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_check_in_events_tenant_id_scanner_capability_id",
                table: "ie_admission_check_in_events",
                columns: new[] { "tenant_id", "scanner_capability_id" });

            migrationBuilder.CreateIndex(
                name: "ux_admission_check_in_policies_target",
                table: "ie_admission_check_in_policies",
                columns: new[] { "tenant_id", "admission_target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_check_in_states_tenant_id_admission_target_id",
                table: "ie_admission_check_in_states",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_check_in_states_tenant_id_admission_tic_A2B5C988",
                table: "ie_admission_check_in_states",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_check_in_states_tenant_id_admission_tic_F8A3D9F9",
                table: "ie_admission_check_in_states",
                columns: new[] { "tenant_id", "admission_ticket_id", "admission_target_id", "active_check_in_event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_scanner_capabilities_issued_by_actor_id",
                table: "ie_admission_scanner_capabilities",
                column: "issued_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_scanner_capabilities_revoked_by_actor_id",
                table: "ie_admission_scanner_capabilities",
                column: "revoked_by_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_scanner_capabilities_tenant_id_admissio_5B998051",
                table: "ie_admission_scanner_capabilities",
                columns: new[] { "tenant_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_scanner_capabilities_tenant_id_event_id_7943FB1E",
                table: "ie_admission_scanner_capabilities",
                columns: new[] { "tenant_id", "event_id", "admission_target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_scanner_capabilities_tenant_id_lookup_k_8E44787B",
                table: "ie_admission_scanner_capabilities",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admission_scanner_capabilities_issue_request",
                table: "ie_admission_scanner_capabilities",
                columns: new[] { "tenant_id", "issue_request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_targets_tenant_id_event_id_admission_ta_2064A30B",
                table: "ie_admission_targets",
                columns: new[] { "tenant_id", "event_id", "admission_target_type_id", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_targets_tenant_id_event_id_event_day_id",
                table: "ie_admission_targets",
                columns: new[] { "tenant_id", "event_id", "event_day_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_targets_tenant_id_event_id_event_session_id",
                table: "ie_admission_targets",
                columns: new[] { "tenant_id", "event_id", "event_session_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_admission_check_in_policies");

            migrationBuilder.DropTable(
                name: "ie_admission_check_in_states");

            migrationBuilder.DropTable(
                name: "ie_admission_check_in_events");

            migrationBuilder.DropTable(
                name: "ie_admission_scanner_capabilities");

            migrationBuilder.DropTable(
                name: "ie_admission_targets");

            migrationBuilder.DropIndex(
                name: "IX_ie_ticket_type_entitlements_tenant_id_ticket_type_id_47B23051",
                table: "ie_ticket_type_entitlements");

            migrationBuilder.DropColumn(
                name: "scope_id",
                table: "ie_ticket_type_entitlements");

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticket_type_entitlements_tenant_id_ticket_type_id",
                table: "ie_ticket_type_entitlements",
                columns: new[] { "tenant_id", "ticket_type_id" });
        }
    }
}
