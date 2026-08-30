using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketingRecoveryAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_ticketing_recovery_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recovery_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    manifest_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    release_revision = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    schema_revision = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    database_checkpoint = table.Column<long>(type: "INTEGER", nullable: false),
                    object_cutoff_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    retained_key_version = table.Column<int>(type: "INTEGER", nullable: false),
                    authority_floor = table.Column<long>(type: "INTEGER", nullable: false),
                    provider_cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    idempotency_floor = table.Column<long>(type: "INTEGER", nullable: false),
                    worker_fence = table.Column<long>(type: "INTEGER", nullable: false),
                    capability_generation = table.Column<int>(type: "INTEGER", nullable: false),
                    credential_generation = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    validated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    authority_rotated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    workers_opened_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    sales_opened_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    failure_code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_checkpoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ie_ticketing_recovery_reissue_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recovery_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    required_credential_generation = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_reissue_intents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_recovery_operation_id",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "recovery_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_status",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_reissue_intents_tenant_id_recovery_operation_id_admission_ticket_id",
                table: "ie_ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "recovery_operation_id", "admission_ticket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_reissue_intents_tenant_id_status",
                table: "ie_ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_ticketing_recovery_checkpoints");

            migrationBuilder.DropTable(
                name: "ie_ticketing_recovery_reissue_intents");
        }
    }
}
