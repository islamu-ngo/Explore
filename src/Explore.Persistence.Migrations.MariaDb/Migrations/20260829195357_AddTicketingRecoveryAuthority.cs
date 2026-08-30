using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.MariaDb.Migrations
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
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    recovery_operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    manifest_digest = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    release_revision = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    schema_revision = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    database_checkpoint = table.Column<long>(type: "bigint", nullable: false),
                    object_cutoff_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    retained_key_version = table.Column<int>(type: "int", nullable: false),
                    authority_floor = table.Column<long>(type: "bigint", nullable: false),
                    provider_cursor = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_floor = table.Column<long>(type: "bigint", nullable: false),
                    worker_fence = table.Column<long>(type: "bigint", nullable: false),
                    capability_generation = table.Column<int>(type: "int", nullable: false),
                    credential_generation = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    validated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    authority_rotated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    workers_opened_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sales_opened_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    failure_code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_checkpoints", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ie_ticketing_recovery_reissue_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    tenant_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    recovery_operation_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    admission_ticket_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    required_credential_generation = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_ticketing_recovery_reissue_intents", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticketing_recovery_checkpoints_tenant_id_recovery_c44e37e9",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "recovery_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticketing_recovery_checkpoints_tenant_id_status",
                table: "ie_ticketing_recovery_checkpoints",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ie_ticketing_recovery_reissue_intents_tenant_id_reco_a6bb0d1d",
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
