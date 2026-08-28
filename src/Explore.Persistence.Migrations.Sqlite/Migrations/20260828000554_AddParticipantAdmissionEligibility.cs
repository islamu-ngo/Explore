using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
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
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_order_line_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    registration_ticket_assignment_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    participant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    subject_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    requirements_completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    subject_consent_record_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    subject_consent_granted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    consent_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    approval_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    approved_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    approved_by_actor_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    revoked_by_actor_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_participant_admission_eligibilities", x => x.id);
                    table.UniqueConstraint("ak_participant_admission_eligibilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_participant_admission_approval", "(approved_at IS NULL AND approved_by_actor_id IS NULL) OR (approved_at IS NOT NULL AND approved_by_actor_id IS NOT NULL)");
                    table.CheckConstraint("ck_participant_admission_completion_consent", "(subject_consent_record_id IS NULL AND subject_consent_granted_at IS NULL) OR (subject_consent_record_id IS NOT NULL AND subject_consent_granted_at IS NOT NULL)");
                    table.CheckConstraint("ck_participant_admission_revocation", "(revoked_at IS NULL AND revoked_by_actor_id IS NULL) OR (revoked_at IS NOT NULL AND revoked_by_actor_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_ie_participant_admission_eligibilities_registration_consent_records_tenant_id_subject_consent_record_id",
                        columns: x => new { x.tenant_id, x.subject_consent_record_id },
                        principalTable: "ie_registration_consent_records",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_participant_admission_eligibilities_registration_participants_tenant_id_registration_order_id_participant_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.participant_id },
                        principalTable: "ie_registration_participants",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_participant_admission_eligibilities_registration_ticket_assignments_tenant_id_registration_order_id_registration_ticket_assignment_id_registration_order_line_id",
                        columns: x => new { x.tenant_id, x.registration_order_id, x.registration_ticket_assignment_id, x.registration_order_line_id },
                        principalTable: "ie_registration_ticket_assignments",
                        principalColumns: new[] { "tenant_id", "registration_order_id", "id", "registration_order_line_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_participant_admission_eligibilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ie_participant_admission_eligibilities_users_subject_user_id",
                        column: x => x.subject_user_id,
                        principalTable: "ie_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_subject_user_id",
                table: "ie_participant_admission_eligibilities",
                column: "subject_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_tenant_id_event_id",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_tenant_id_registration_order_id_participant_id",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "registration_order_id", "participant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_tenant_id_registration_order_id_registration_ticket_assignment_id_registration_order_line_id",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "registration_order_id", "registration_ticket_assignment_id", "registration_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_tenant_id_registration_ticket_assignment_id",
                table: "ie_participant_admission_eligibilities",
                columns: new[] { "tenant_id", "registration_ticket_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ie_participant_admission_eligibilities_tenant_id_subject_consent_record_id",
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
