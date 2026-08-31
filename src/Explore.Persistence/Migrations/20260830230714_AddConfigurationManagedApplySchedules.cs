using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationManagedApplySchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuration_managed_apply_schedules",
                schema: "islamu_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_authority_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    artifact_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    target_revision_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    managed_plan_digest = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_by = table.Column<Guid>(type: "uuid", nullable: true),
                    apply_not_before = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    apply_before = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuration_managed_apply_schedules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_managed_apply_schedules_target_au_ce143e268235",
                schema: "islamu_event",
                table: "configuration_managed_apply_schedules",
                columns: new[] { "target_authority_key", "status", "apply_not_before" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuration_managed_apply_schedules",
                schema: "islamu_event");
        }
    }
}
