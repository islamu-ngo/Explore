using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrivacyErasureLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at_utc",
                table: "privacy_erasure_sagas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "completed_provider_work_count",
                table: "privacy_erasure_sagas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "local_settled_at_utc",
                table: "privacy_erasure_sagas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "provider_work_count",
                table: "privacy_erasure_sagas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "status",
                table: "privacy_erasure_sagas",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "privacy_erasure_sagas",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "privacy_erasure_provider_work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_kind = table.Column<short>(type: "smallint", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_kind = table.Column<short>(type: "smallint", nullable: false),
                    action = table.Column<short>(type: "smallint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lease_owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_fence = table.Column<long>(type: "bigint", nullable: false),
                    lease_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    unknown_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_erasure_provider_work", x => x.id);
                    table.CheckConstraint("ck_privacy_erasure_provider_work_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_privacy_erasure_provider_work_lease_fence", "lease_fence >= 0");
                    table.CheckConstraint("ck_privacy_erasure_provider_work_subject_kind", "subject_kind = 1");
                    table.ForeignKey(
                        name: "fk_privacy_erasure_provider_work_privacy_erasure_sagas_intent_",
                        column: x => x.intent_id,
                        principalTable: "privacy_erasure_sagas",
                        principalColumn: "intent_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_erasure_sagas_subject_kind_subject_id",
                table: "privacy_erasure_sagas",
                columns: new[] { "subject_kind", "subject_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_sagas_provider_counts",
                table: "privacy_erasure_sagas",
                sql: "provider_work_count >= 0 AND completed_provider_work_count >= 0 AND completed_provider_work_count <= provider_work_count");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_sagas_status",
                table: "privacy_erasure_sagas",
                sql: "status IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_erasure_provider_work_intent_id_provider_kind_actio",
                table: "privacy_erasure_provider_work",
                columns: new[] { "intent_id", "provider_kind", "action", "tenant_id", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_privacy_erasure_provider_work_status_next_attempt_at_utc_le",
                table: "privacy_erasure_provider_work",
                columns: new[] { "status", "next_attempt_at_utc", "lease_expires_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "privacy_erasure_provider_work");

            migrationBuilder.DropIndex(
                name: "ix_privacy_erasure_sagas_subject_kind_subject_id",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_sagas_provider_counts",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_sagas_status",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropColumn(
                name: "completed_at_utc",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropColumn(
                name: "completed_provider_work_count",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropColumn(
                name: "local_settled_at_utc",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropColumn(
                name: "provider_work_count",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropColumn(
                name: "status",
                table: "privacy_erasure_sagas");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "privacy_erasure_sagas");
        }
    }
}
