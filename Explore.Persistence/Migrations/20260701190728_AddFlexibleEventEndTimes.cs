// ABOUTME: EF Core Migration adding flexible event end times and check constraints.
// ABOUTME: Maps EndTimeType, EndReferencePrayer, and EndOffsetMinutes fields.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleEventEndTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "end_time_type",
                table: "event_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "end_offset_minutes",
                table: "event_session_islamic_aspects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "end_reference_prayer",
                table: "event_session_islamic_aspects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_consent_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    field_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    provider_trust_tier_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    purpose = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_consent_grants", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_consent_grants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ai_consent_grants_users_subject_user_id",
                        column: x => x.subject_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSession_EndTimeTypeState",
                table: "event_sessions",
                sql: "start_time IS NULL OR ((end_time_type = 0 AND end_time IS NOT NULL) OR (end_time_type = 1 AND end_time IS NULL) OR (end_time_type = 2))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSessionIslamicAspect_EndOffsetRange",
                table: "event_session_islamic_aspects",
                sql: "end_offset_minutes IS NULL OR end_offset_minutes BETWEEN -180 AND 180");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSessionIslamicAspect_EndReferencePrayerRange",
                table: "event_session_islamic_aspects",
                sql: "end_reference_prayer IS NULL OR end_reference_prayer BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSessionIslamicAspect_EndTimeState",
                table: "event_session_islamic_aspects",
                sql: "((end_reference_prayer IS NULL AND end_offset_minutes IS NULL) OR (end_reference_prayer IS NOT NULL AND end_offset_minutes IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_AiConsentGrants_Subject_Entity_Field_Tier",
                table: "ai_consent_grants",
                columns: new[] { "subject_user_id", "entity_name", "field_name", "provider_trust_tier_id" });

            migrationBuilder.CreateIndex(
                name: "IX_AiConsentGrants_TenantId",
                table: "ai_consent_grants",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_consent_grants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSession_EndTimeTypeState",
                table: "event_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSessionIslamicAspect_EndOffsetRange",
                table: "event_session_islamic_aspects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSessionIslamicAspect_EndReferencePrayerRange",
                table: "event_session_islamic_aspects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSessionIslamicAspect_EndTimeState",
                table: "event_session_islamic_aspects");

            migrationBuilder.DropColumn(
                name: "end_time_type",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "end_offset_minutes",
                table: "event_session_islamic_aspects");

            migrationBuilder.DropColumn(
                name: "end_reference_prayer",
                table: "event_session_islamic_aspects");
        }
    }
}
