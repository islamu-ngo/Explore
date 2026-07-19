// ABOUTME: Allows a changed immutable ATProto payload to supersede a terminal attempt for the same source version.
// ABOUTME: Keeps exact payload attempts unique so durable RSVP reconciliation cannot hot-loop hard failures.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260719130000_IncludePdsPayloadHashInSourceVersionUniqueness")]
public partial class IncludePdsPayloadHashInSourceVersionUniqueness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "depends_on_cid",
            table: "pds_sync_outbox",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.DropIndex(
            name: "ux_pds_sync_outbox_source_version",
            table: "pds_sync_outbox");

        migrationBuilder.CreateIndex(
            name: "ux_pds_sync_outbox_source_version",
            table: "pds_sync_outbox",
            columns:
            [
                "tenant_id",
                "source_entity_type",
                "source_entity_id",
                "source_version",
                "operation",
                "payload_hash"
            ],
            unique: true,
            filter: "status IN (1, 2) AND superseded_at IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_pds_sync_outbox_source_version",
            table: "pds_sync_outbox");

        migrationBuilder.CreateIndex(
            name: "ux_pds_sync_outbox_source_version",
            table: "pds_sync_outbox",
            columns:
            [
                "tenant_id",
                "source_entity_type",
                "source_entity_id",
                "source_version",
                "operation"
            ],
            unique: true);

        migrationBuilder.DropColumn(
            name: "depends_on_cid",
            table: "pds_sync_outbox");
    }
}
