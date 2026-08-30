using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketingRecoveryAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticketing_recovery_checkpoints",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovery_operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manifest_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    release_revision = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    schema_revision = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    database_checkpoint = table.Column<long>(type: "bigint", nullable: false),
                    object_cutoff_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retained_key_version = table.Column<int>(type: "integer", nullable: false),
                    authority_floor = table.Column<long>(type: "bigint", nullable: false),
                    provider_cursor = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_floor = table.Column<long>(type: "bigint", nullable: false),
                    worker_fence = table.Column<long>(type: "bigint", nullable: false),
                    capability_generation = table.Column<int>(type: "integer", nullable: false),
                    credential_generation = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    authority_rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    workers_opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sales_opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticketing_recovery_checkpoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticketing_recovery_reissue_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovery_operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_credential_generation = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticketing_recovery_reissue_intents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_recove_382a2690a057",
                schema: "islamu_event",
                table: "ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "recovery_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_status",
                schema: "islamu_event",
                table: "ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_reissue_intents_tenant_id_re_1d88fa84b407",
                schema: "islamu_event",
                table: "ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "recovery_operation_id", "admission_ticket_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_reissue_intents_tenant_id_status",
                schema: "islamu_event",
                table: "ticketing_recovery_reissue_intents",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticketing_recovery_checkpoints",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "ticketing_recovery_reissue_intents",
                schema: "islamu_event");
        }
    }
}
