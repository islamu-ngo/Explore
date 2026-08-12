using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationProviderSubmissionWriteEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_registration_submissions_native_identity",
                schema: "islamu_event",
                table: "registration_submissions");

            migrationBuilder.DropIndex(
                name: "ux_registration_submissions_provider_identity",
                schema: "islamu_event",
                table: "registration_submissions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                schema: "islamu_event",
                table: "registration_submissions");

            migrationBuilder.CreateTable(
                name: "registration_provider_submission_write_effects",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_attempt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_submission_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_provider_binding_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    processing_fence = table.Column<long>(type: "bigint", nullable: false),
                    processing_lease_owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    processing_lease_token = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processing_lease_expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    next_attempt_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    parked_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    failure_code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registration_provider_submission_write_effects", x => x.id);
                    table.UniqueConstraint("ak_registration_provider_submission_write_effects_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_registration_provider_submission_write_effects_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_registration_provider_submission_write_effects_processing_fence", "processing_fence >= 0");
                    table.ForeignKey(
                        name: "fk_registration_provider_submission_write_effects_registration_orders_tenant_id_event_id_registration_order_id",
                        columns: x => new { x.tenant_id, x.event_id, x.registration_order_id },
                        principalSchema: "islamu_event",
                        principalTable: "registration_orders",
                        principalColumns: new[] { "tenant_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registration_provider_submission_write_effects_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "islamu_event",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_registration_submissions_native_identity",
                schema: "islamu_event",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "registration_attempt_id", "business_deduplication_key" },
                unique: true,
                filter: "provider_submission_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_registration_submissions_provider_identity",
                schema: "islamu_event",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_submission_id", "provider_response_revision" },
                unique: true,
                filter: "provider_submission_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                schema: "islamu_event",
                table: "registration_submissions",
                sql: "(registration_provider_binding_id IS NULL AND provider_mapping_revision_hash IS NULL AND provider_submission_id IS NULL AND provider_response_revision IS NULL) OR (registration_provider_binding_id IS NOT NULL AND provider_mapping_revision_hash IS NOT NULL AND ((provider_submission_id IS NULL AND provider_response_revision IS NULL) OR (provider_submission_id IS NOT NULL AND provider_response_revision IS NOT NULL)))");

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_submission_write_effects_tenant_id_event_id_registration_order_id",
                schema: "islamu_event",
                table: "registration_provider_submission_write_effects",
                columns: new[] { "tenant_id", "event_id", "registration_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registration_provider_submission_write_effects_worker_poll",
                schema: "islamu_event",
                table: "registration_provider_submission_write_effects",
                columns: new[] { "status", "next_attempt_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_registration_provider_submission_write_effects_submission",
                schema: "islamu_event",
                table: "registration_provider_submission_write_effects",
                columns: new[] { "tenant_id", "registration_submission_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_provider_submission_write_effects",
                schema: "islamu_event");

            migrationBuilder.DropIndex(
                name: "ux_registration_submissions_native_identity",
                schema: "islamu_event",
                table: "registration_submissions");

            migrationBuilder.DropIndex(
                name: "ux_registration_submissions_provider_identity",
                schema: "islamu_event",
                table: "registration_submissions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                schema: "islamu_event",
                table: "registration_submissions");

            migrationBuilder.CreateIndex(
                name: "ux_registration_submissions_native_identity",
                schema: "islamu_event",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "registration_attempt_id", "business_deduplication_key" },
                unique: true,
                filter: "registration_provider_binding_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_registration_submissions_provider_identity",
                schema: "islamu_event",
                table: "registration_submissions",
                columns: new[] { "tenant_id", "registration_provider_binding_id", "provider_submission_id", "provider_response_revision" },
                unique: true,
                filter: "registration_provider_binding_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_registration_submissions_provider_tuple",
                schema: "islamu_event",
                table: "registration_submissions",
                sql: "(registration_provider_binding_id IS NULL AND provider_mapping_revision_hash IS NULL AND provider_submission_id IS NULL AND provider_response_revision IS NULL) OR (registration_provider_binding_id IS NOT NULL AND provider_mapping_revision_hash IS NOT NULL AND provider_submission_id IS NOT NULL AND provider_response_revision IS NOT NULL)");
        }
    }
}
