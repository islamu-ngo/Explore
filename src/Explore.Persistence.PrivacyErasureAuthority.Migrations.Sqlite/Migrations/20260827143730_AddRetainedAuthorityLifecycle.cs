using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddRetainedAuthorityLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_erasure_intents_intent_uuid_v7",
                table: "ie_erasure_intents");

            migrationBuilder.AddColumn<bool>(
                name: "is_legal_hold_pseudonymized",
                table: "ie_erasure_intents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "retained_floor_sequence",
                table: "ie_authority_counter",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddCheckConstraint(
                name: "ck_erasure_intents_intent_uuid_v7",
                table: "ie_erasure_intents",
                sql: "is_legal_hold_pseudonymized = 1 OR substr(intent_id, 15, 1) = '7'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authority_counter_retained_floor",
                table: "ie_authority_counter",
                sql: "retained_floor_sequence >= 0 AND retained_floor_sequence <= last_sequence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_erasure_intents_intent_uuid_v7",
                table: "ie_erasure_intents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authority_counter_retained_floor",
                table: "ie_authority_counter");

            migrationBuilder.DropColumn(
                name: "is_legal_hold_pseudonymized",
                table: "ie_erasure_intents");

            migrationBuilder.DropColumn(
                name: "retained_floor_sequence",
                table: "ie_authority_counter");

            migrationBuilder.AddCheckConstraint(
                name: "ck_erasure_intents_intent_uuid_v7",
                table: "ie_erasure_intents",
                sql: "substr(intent_id, 15, 1) = '7'");
        }
    }
}
