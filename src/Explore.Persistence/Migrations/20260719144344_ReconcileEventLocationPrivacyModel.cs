using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileEventLocationPrivacyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "smtp_available_tokens",
                table: "email_dispatch_tenant_controls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "smtp_refill_at",
                table: "email_dispatch_tenant_controls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "atproto_event_projections",
                columns: table => new
                {
                    atproto_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    mode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    rsvp_expected = table.Column<bool>(type: "boolean", nullable: true),
                    location_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    source_version = table.Column<long>(type: "bigint", nullable: false),
                    materialized_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_atproto_event_projections", x => x.atproto_record_id);
                    table.CheckConstraint("ck_atproto_event_projections_source_version", "source_version >= 0");
                    table.CheckConstraint("ck_atproto_event_projections_time_order", "ends_at IS NULL OR starts_at IS NULL OR ends_at > starts_at");
                    table.ForeignKey(
                        name: "fk_atproto_event_projections_atproto_records_atproto_record_id",
                        column: x => x.atproto_record_id,
                        principalTable: "atproto_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_rate_pair",
                table: "email_dispatch_tenant_controls",
                sql: "(smtp_available_tokens IS NULL) = (smtp_refill_at IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_tokens_nonnegative",
                table: "email_dispatch_tenant_controls",
                sql: "smtp_available_tokens IS NULL OR smtp_available_tokens >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_atproto_event_projections_created_at",
                table: "atproto_event_projections",
                columns: new[] { "created_at", "atproto_record_id" });

            migrationBuilder.CreateIndex(
                name: "ix_atproto_event_projections_name",
                table: "atproto_event_projections",
                columns: new[] { "name", "atproto_record_id" });

            migrationBuilder.CreateIndex(
                name: "ix_atproto_event_projections_starts_at",
                table: "atproto_event_projections",
                columns: new[] { "starts_at", "atproto_record_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "atproto_event_projections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_rate_pair",
                table: "email_dispatch_tenant_controls");

            migrationBuilder.DropCheckConstraint(
                name: "ck_email_dispatch_tenant_controls_smtp_tokens_nonnegative",
                table: "email_dispatch_tenant_controls");

            migrationBuilder.DropColumn(
                name: "smtp_available_tokens",
                table: "email_dispatch_tenant_controls");

            migrationBuilder.DropColumn(
                name: "smtp_refill_at",
                table: "email_dispatch_tenant_controls");
        }
    }
}
