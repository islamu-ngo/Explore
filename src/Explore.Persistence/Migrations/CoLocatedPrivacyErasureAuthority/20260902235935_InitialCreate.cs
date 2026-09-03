using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.CoLocatedPrivacyErasureAuthority
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "islamu_event");

            migrationBuilder.CreateTable(
                name: "authority_counter",
                schema: "islamu_event",
                columns: table => new
                {
                    singleton = table.Column<bool>(type: "boolean", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false),
                    retained_floor_sequence = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authority_counter", x => x.singleton);
                    table.CheckConstraint("ck_privacy_erasure_authority_counter_nonnegative", "last_sequence >= 0");
                    table.CheckConstraint("ck_privacy_erasure_authority_counter_retained_floor", "retained_floor_sequence >= 0 AND retained_floor_sequence <= last_sequence");
                    table.CheckConstraint("ck_privacy_erasure_authority_counter_singleton", "singleton");
                });

            migrationBuilder.CreateTable(
                name: "erasure_intents",
                schema: "islamu_event",
                columns: table => new
                {
                    authority_sequence = table.Column<long>(type: "bigint", nullable: false),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_kind = table.Column<short>(type: "smallint", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<short>(type: "smallint", nullable: false),
                    policy_version = table.Column<int>(type: "integer", nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retention_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "'infinity'::timestamp with time zone"),
                    is_legal_hold_pseudonymized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_erasure_intents", x => x.authority_sequence);
                    table.CheckConstraint("ck_privacy_erasure_intents_intent_rfc4122_variant", "substring(intent_id::text, 20, 1) IN ('8', '9', 'a', 'b')");
                    table.CheckConstraint("ck_privacy_erasure_intents_intent_uuid_v7", "is_legal_hold_pseudonymized OR substring(intent_id::text, 15, 1) = '7'");
                    table.CheckConstraint("ck_privacy_erasure_intents_policy_version", "policy_version > 0");
                    table.CheckConstraint("ck_privacy_erasure_intents_reason", "reason_code BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_privacy_erasure_intents_retention", "retention_expires_at_utc > recorded_at_utc");
                    table.CheckConstraint("ck_privacy_erasure_intents_sequence", "authority_sequence > 0");
                    table.CheckConstraint("ck_privacy_erasure_intents_server_time_order", "recorded_at_utc >= requested_at_utc");
                    table.CheckConstraint("ck_privacy_erasure_intents_subject_kind", "subject_kind = 1");
                    table.CheckConstraint("ck_privacy_erasure_intents_subject_nonempty", "subject_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateIndex(
                name: "ix_erasure_intents_intent_id_subject_kind_policy_version",
                schema: "islamu_event",
                table: "erasure_intents",
                columns: new[] { "intent_id", "subject_kind", "policy_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authority_counter",
                schema: "islamu_event");

            migrationBuilder.DropTable(
                name: "erasure_intents",
                schema: "islamu_event");
        }
    }
}
