using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialEmbeddedPrivacyErasureAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authority_counter",
                columns: table => new
                {
                    singleton = table.Column<bool>(type: "INTEGER", nullable: false),
                    last_sequence = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authority_counter", x => x.singleton);
                    table.CheckConstraint("ck_authority_counter_nonnegative", "last_sequence >= 0");
                    table.CheckConstraint("ck_authority_counter_singleton", "singleton = 1");
                });

            migrationBuilder.CreateTable(
                name: "erasure_intents",
                columns: table => new
                {
                    authority_sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    intent_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    subject_kind = table.Column<short>(type: "INTEGER", nullable: false),
                    subject_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    reason_code = table.Column<short>(type: "INTEGER", nullable: false),
                    policy_version = table.Column<int>(type: "INTEGER", nullable: false),
                    requested_at_utc = table.Column<long>(type: "INTEGER", nullable: false),
                    recorded_at_utc = table.Column<long>(type: "INTEGER", nullable: false),
                    retention_expires_at_utc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_erasure_intents", x => x.authority_sequence);
                    table.UniqueConstraint("ak_erasure_intents_intent_id", x => x.intent_id);
                    table.CheckConstraint("ck_erasure_intents_intent_uuid_v7", "substr(intent_id, 15, 1) = '7'");
                    table.CheckConstraint("ck_erasure_intents_intent_variant", "lower(substr(intent_id, 20, 1)) IN ('8', '9', 'a', 'b')");
                    table.CheckConstraint("ck_erasure_intents_policy_version", "policy_version > 0");
                    table.CheckConstraint("ck_erasure_intents_reason", "reason_code BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_erasure_intents_retention", "retention_expires_at_utc > recorded_at_utc");
                    table.CheckConstraint("ck_erasure_intents_sequence", "authority_sequence > 0");
                    table.CheckConstraint("ck_erasure_intents_server_time_order", "recorded_at_utc >= requested_at_utc");
                    table.CheckConstraint("ck_erasure_intents_subject_kind", "subject_kind = 1");
                    table.CheckConstraint("ck_erasure_intents_subject_nonempty", "subject_id <> '00000000-0000-0000-0000-000000000000'");
                });

            migrationBuilder.CreateIndex(
                name: "ix_erasure_intents_intent_id_subject_kind_policy_version",
                table: "erasure_intents",
                columns: new[] { "intent_id", "subject_kind", "policy_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authority_counter");

            migrationBuilder.DropTable(
                name: "erasure_intents");
        }
    }
}
