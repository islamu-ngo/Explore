using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionIssuancePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_credential_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_credential_statuses", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_statuses", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_transition_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    master_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_transition_reasons", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_order_line_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    participant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    ticket_catalog_version_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    event_ticket_type_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    display_reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    admission_ticket_status_id = table.Column<int>(type: "int", nullable: false),
                    last_transition_reason_id = table.Column<int>(type: "int", nullable: false),
                    last_transition_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_tickets", x => x.id);
                    table.UniqueConstraint("ak_admission_tickets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_admission_ticket_statuses_ad_A6BCCD5A",
                        column: x => x.admission_ticket_status_id,
                        principalTable: "ie_admission_ticket_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_admission_ticket_transition__38739B6B",
                        column: x => x.last_transition_reason_id,
                        principalTable: "ie_admission_ticket_transition_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_event_ticket_catalog_version_DB4B0BC1",
                        columns: x => new { x.tenant_id, x.ticket_catalog_version_id },
                        principalTable: "ie_event_ticket_catalog_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_event_ticket_types_tenant_id_13AB1D4A",
                        columns: x => new { x.tenant_id, x.event_ticket_type_id },
                        principalTable: "ie_event_ticket_types",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_registration_order_lines_ten_9F2C5786",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_order_line_id },
                        principalTable: "ie_registration_order_lines",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_registration_orders_tenant_i_5786EE08",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_registration_participants_te_ED140969",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_tickets_ie_registration_ticket_assignme_1704F1DE",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_tickets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_delivery_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    finalization_effect_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    protected_credential = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    protection_version = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    routed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    handoff_completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    handoff_receipt_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_delivery_intents", x => x.id);
                    table.UniqueConstraint("ak_admission_delivery_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_delivery_intents_handoff_receipt", "(handoff_completed_at IS NULL AND handoff_receipt_id IS NULL) OR (handoff_completed_at IS NOT NULL AND handoff_receipt_id IS NOT NULL)");
                    table.CheckConstraint("ck_admission_delivery_intents_protection_version", "protection_version > 0");
                    table.ForeignKey(
                        name: "FK_ie_admission_delivery_intents_ie_admission_tickets_t_7C8C30B9",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_delivery_intents_ie_registration_finali_3EC344FD",
                        columns: x => new { x.tenant_id, x.finalization_effect_id },
                        principalTable: "ie_registration_finalization_effects",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_delivery_intents_ie_registration_ticket_3FEC6E73",
                        columns: x => new { x.tenant_id, x.registration_ticket_assignment_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_admission_delivery_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_admission_ticket_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    credential_version = table.Column<int>(type: "int", nullable: false),
                    lookup_key_version = table.Column<int>(type: "int", nullable: false),
                    lookup_digest = table.Column<string>(type: "char(44)", fixedLength: true, maxLength: 44, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    admission_ticket_credential_status_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    active_uniqueness_slot = table.Column<int>(type: "int", nullable: false, computedColumnSql: "CASE WHEN admission_ticket_credential_status_id = 1 THEN 0 ELSE credential_version END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_admission_ticket_credentials", x => x.id);
                    table.UniqueConstraint("ak_admission_ticket_credentials_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_admission_ticket_credentials_versions", "credential_version > 0 AND lookup_key_version > 0");
                    table.ForeignKey(
                        name: "FK_ie_admission_ticket_credentials_ie_admission_ticket__5584FA56",
                        column: x => x.admission_ticket_credential_status_id,
                        principalTable: "ie_admission_ticket_credential_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_admission_ticket_credentials_ie_admission_tickets_C87CCEE8",
                        columns: x => new { x.tenant_id, x.admission_ticket_id },
                        principalTable: "ie_admission_tickets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_delivery_intents_handoff_completed_at_r_D0860C4C",
                table: "ie_admission_delivery_intents",
                columns: new[] { "handoff_completed_at", "routed_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_delivery_intents_tenant_id_admission_ticket_id",
                table: "ie_admission_delivery_intents",
                columns: new[] { "tenant_id", "admission_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_delivery_intents_tenant_id_finalization_D2499D4A",
                table: "ie_admission_delivery_intents",
                columns: new[] { "tenant_id", "finalization_effect_id", "registration_ticket_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_delivery_intents_tenant_id_registration_6251DC31",
                table: "ie_admission_delivery_intents",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_credential_statuses_master_code",
                table: "ie_admission_ticket_credential_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_ticket_credentials_admission_ticket_cre_038D8738",
                table: "ie_admission_ticket_credentials",
                column: "admission_ticket_credential_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_ticket_credentials_tenant_id_admission__D693E7C0",
                table: "ie_admission_ticket_credentials",
                columns: new[] { "tenant_id", "admission_ticket_id", "credential_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_ticket_credentials_tenant_id_admission__EE2A0241",
                table: "ie_admission_ticket_credentials",
                columns: new[] { "tenant_id", "admission_ticket_id", "active_uniqueness_slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_ticket_credentials_tenant_id_lookup_key_0C73CCD5",
                table: "ie_admission_ticket_credentials",
                columns: new[] { "tenant_id", "lookup_key_version", "lookup_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_statuses_master_code",
                table: "ie_admission_ticket_statuses",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_ticket_transition_reasons_master_code",
                table: "ie_admission_ticket_transition_reasons",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_admission_ticket_status_id",
                table: "ie_admission_tickets",
                column: "admission_ticket_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_last_transition_reason_id",
                table: "ie_admission_tickets",
                column: "last_transition_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_event_id_registration_order_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "event_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_event_ticket_type_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "event_ticket_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_tickets_tenant_id_registration_order_id_759D374A",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "participant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_tickets_tenant_id_registration_order_id_9866698F",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_tickets_tenant_id_registration_order_id_EBD91BD4",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_order_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_admission_tickets_tenant_id_registration_ticket_a_3058A578",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_admission_tickets_tenant_id_ticket_catalog_version_id",
                table: "ie_admission_tickets",
                columns: new[] { "tenant_id", "ticket_catalog_version_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_admission_delivery_intents");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_credentials");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_credential_statuses");

            migrationBuilder.DropTable(
                name: "ie_admission_tickets");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_statuses");

            migrationBuilder.DropTable(
                name: "ie_admission_ticket_transition_reasons");
        }
    }
}
