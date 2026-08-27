using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.CoLocatedPrivacyErasureAuthority
{
    /// <inheritdoc />
    public partial class AddRetainedAuthorityLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_intents_intent_uuid_v7",
                schema: "islamu_event",
                table: "erasure_intents");

            migrationBuilder.AddColumn<bool>(
                name: "is_legal_hold_pseudonymized",
                schema: "islamu_event",
                table: "erasure_intents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "retained_floor_sequence",
                schema: "islamu_event",
                table: "authority_counter",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_intents_intent_uuid_v7",
                schema: "islamu_event",
                table: "erasure_intents",
                sql: "is_legal_hold_pseudonymized OR substring(intent_id::text, 15, 1) = '7'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_authority_counter_retained_floor",
                schema: "islamu_event",
                table: "authority_counter",
                sql: "retained_floor_sequence >= 0 AND retained_floor_sequence <= last_sequence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_intents_intent_uuid_v7",
                schema: "islamu_event",
                table: "erasure_intents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_privacy_erasure_authority_counter_retained_floor",
                schema: "islamu_event",
                table: "authority_counter");

            migrationBuilder.DropColumn(
                name: "is_legal_hold_pseudonymized",
                schema: "islamu_event",
                table: "erasure_intents");

            migrationBuilder.DropColumn(
                name: "retained_floor_sequence",
                schema: "islamu_event",
                table: "authority_counter");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privacy_erasure_intents_intent_uuid_v7",
                schema: "islamu_event",
                table: "erasure_intents",
                sql: "substring(intent_id::text, 15, 1) = '7'");
        }
    }
}
