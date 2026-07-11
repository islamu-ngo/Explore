// ABOUTME: Adds server-owned public event codes used by clean public event URLs.
// ABOUTME: Backfills existing rows deterministically before enforcing tenant-scoped uniqueness.

using Explore.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260705120000_AddEventPublicCode")]
public partial class AddEventPublicCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "public_code",
            table: "events",
            type: "character varying(12)",
            maxLength: 12,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE events
            SET public_code = lower(substr(replace(id::text, '-', ''), 1, 12))
            WHERE public_code IS NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "public_code",
            table: "events",
            type: "character varying(12)",
            maxLength: 12,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(12)",
            oldMaxLength: 12,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_events_tenant_public_code",
            table: "events",
            columns: new[] { "tenant_id", "public_code" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_events_tenant_public_code",
            table: "events");

        migrationBuilder.DropColumn(
            name: "public_code",
            table: "events");
    }
}
