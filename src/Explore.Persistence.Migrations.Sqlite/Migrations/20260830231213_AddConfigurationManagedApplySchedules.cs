using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationManagedApplySchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ie_configuration_managed_apply_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    target_authority_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    artifact_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    target_revision_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    managed_plan_digest = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "TEXT", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    applied_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    apply_not_before = table.Column<DateTime>(type: "TEXT", nullable: false),
                    apply_before = table.Column<DateTime>(type: "TEXT", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    revision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ie_configuration_managed_apply_schedules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_configuration_managed_apply_schedules_target_authority_key_status_apply_not_before",
                table: "ie_configuration_managed_apply_schedules",
                columns: new[] { "target_authority_key", "status", "apply_not_before" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ie_configuration_managed_apply_schedules");
        }
    }
}
