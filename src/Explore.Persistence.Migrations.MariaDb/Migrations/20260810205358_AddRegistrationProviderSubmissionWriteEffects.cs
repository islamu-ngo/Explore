using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationProviderSubmissionWriteEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_a_C78E61A1",
                table: "ie_registration_submissions");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_p_A92B966C",
                table: "ie_registration_submissions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                table: "ie_registration_submissions");

            migrationBuilder.CreateTable(
                name: "ie_registration_provider_submission_write_effects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    event_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_attempt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_submission_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    registration_provider_binding_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    status = table.Column<int>(type: "int", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    processing_lease_owner = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    processing_lease_token = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    processing_lease_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    next_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    parked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failure_code = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_registration_provider_submission_write_effects", x => x.id);
                    table.UniqueConstraint("AK_ie_registration_provider_submission_write_effects_te_7C158B14", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_provider_submission_write_effects_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_registration_provider_submission_write_effects_processing_fe~", "processing_fence >= 0");
                    table.ForeignKey(
                        name: "FK_ie_registration_provider_submission_write_effects_ie_3967A088",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id },
                        principalTable: "ie_registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ie_registration_provider_submission_write_effects_ie_9FAA9258",
                        column: x => x.tenant_id,
                        principalTable: "ie_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_a_C78E61A1",
                table: "ie_registration_submissions",
                columns: new[] { "tenant_id", "registration_attempt_id", "business_deduplication_key" },
                unique: true,
                filter: "provider_submission_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_p_A92B966C",
                table: "ie_registration_submissions",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_submission_id", "provider_response_revision" },
                unique: true,
                filter: "provider_submission_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                table: "ie_registration_submissions",
                sql: "(registration_provider_binding_id IS NULL AND provider_mapping_revision_hash IS NULL AND provider_submission_id IS NULL AND provider_response_revision IS NULL) OR (registration_provider_binding_id IS NOT NULL AND provider_mapping_revision_hash IS NOT NULL AND ((provider_submission_id IS NULL AND provider_response_revision IS NULL) OR (provider_submission_id IS NOT NULL AND provider_response_revision IS NOT NULL)))");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_submission_write_effects_st_74CB1F27",
                table: "ie_registration_provider_submission_write_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_submission_write_effects_te_103C1B32",
                table: "ie_registration_provider_submission_write_effects",
                columns: new[] { "tenant_id", "event_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_provider_submission_write_effects_te_A5922E85",
                table: "ie_registration_provider_submission_write_effects",
                columns: new[] { "tenant_id", "registration_submission_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_registration_provider_submission_write_effects");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_a_C78E61A1",
                table: "ie_registration_submissions");

            migrationBuilder.DropIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_p_A92B966C",
                table: "ie_registration_submissions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                table: "ie_registration_submissions");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_a_C78E61A1",
                table: "ie_registration_submissions",
                columns: new[] { "tenant_id", "registration_attempt_id", "business_deduplication_key" },
                unique: true,
                filter: "registration_provider_binding_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ie_registration_submissions_tenant_id_registration_p_A92B966C",
                table: "ie_registration_submissions",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_submission_id", "provider_response_revision" },
                unique: true,
                filter: "registration_provider_binding_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                table: "ie_registration_submissions",
                sql: "(registration_provider_binding_id IS NULL AND provider_mapping_revision_hash IS NULL AND provider_submission_id IS NULL AND provider_response_revision IS NULL) OR (registration_provider_binding_id IS NOT NULL AND provider_mapping_revision_hash IS NOT NULL AND provider_submission_id IS NOT NULL AND provider_response_revision IS NOT NULL)");
        }
    }
}
