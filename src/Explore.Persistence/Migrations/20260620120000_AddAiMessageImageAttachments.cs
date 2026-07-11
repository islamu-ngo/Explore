// ABOUTME: Adds persisted AI message image attachment JSON for queued provider requests.
// ABOUTME: Keeps text content unchanged while allowing optional base64 image payload storage.

using Explore.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations;

[DbContext(typeof(ExploreDbContext))]
[Migration("20260620120000_AddAiMessageImageAttachments")]
public partial class AddAiMessageImageAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "image_attachments_json",
            table: "ai_messages",
            type: "jsonb",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "image_attachments_json",
            table: "ai_messages");
    }
}
