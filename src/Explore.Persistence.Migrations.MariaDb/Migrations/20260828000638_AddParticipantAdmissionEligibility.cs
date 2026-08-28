using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantAdmissionEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_registration_consent_records_tenant_id_id",
                table: "ie_registration_consent_records",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "ie_participant_admission_eligibilities",
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
                    subject_user_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    requirements_completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    subject_consent_record_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    subject_consent_granted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    consent_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    approval_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    approved_by_actor_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_actor_id = table.Column<Guid>(type: "char(36)", nullable: true)
                        .Annotation("Relational:Collation", "ascii_general_ci"),
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
                    table.PrimaryKey("pk_ie_participant_admission_eligibilities", x => x.id);
                    table.UniqueConstraint("ak_participant_admission_eligibilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_participant_admission_approval", "(approved_at IS NULL AND approved_by_actor_id IS NULL) OR (approved_at IS NOT NULL AND approved_by_actor_id IS NOT NULL)");
                    table.CheckConstraint("ck_participant_admission_completion_consent", "(subject_consent_record_id IS NULL AND subject_consent_granted_at IS NULL) OR (subject_consent_record_id IS NOT NULL AND subject_consent_granted_at IS NOT NULL)");
                    table.CheckConstraint("ck_participant_admission_revocation", "(revoked_at IS NULL AND revoked_by_actor_id IS NULL) OR (revoked_at IS NOT NULL AND revoked_by_actor_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ie_participant_admission_eligibilities_ie_registrati_0DF572C0",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_participant_admission_eligibilities_ie_registrati_DCA43E38",
                        columns: x => new { x.tenant_id, x.subject_consent_record_id },
                        principalTable: "ie_registration_consent_records",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_participant_admission_eligibilities_ie_registrati_E8D45C37",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_participant_admission_eligibilities_ie_users_subj_48692589",
                        column: x => x.subject_user_id,
                        principalTable: "ie_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_participant_admission_eligibilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_subject_user_id",
                table: "ie_participant_admission_eligibilities",
                column: "subject_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_tenant_id_event_id",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_participant_admission_eligibilities_tenant_id_reg_0621E8D6",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "registration_order_id", "participant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_participant_admission_eligibilities_tenant_id_reg_179DA9B7",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ie_participant_admission_eligibilities_tenant_id_reg_60C6F4A7",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_participant_admission_eligibilities_tenant_id_sub_52BF87EC",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "subject_consent_record_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_participant_admission_eligibilities");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_registration_consent_records_tenant_id_id",
                table: "ie_registration_consent_records");
        }
    }
}
