using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLocationPrivacyExpand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_session_agenda_items_tenant_id_event_session_id",
                table: "event_session_agenda_items");

            migrationBuilder.AddColumn<int>(
                name: "location_kind_id",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "location_privacy_state_id",
                table: "locations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pii_erased_at_utc",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pii_erasure_reason",
                table: "locations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_location_id",
                table: "event_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_location_id",
                table: "event_session_groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_location_id",
                table: "event_session_agenda_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_location_id",
                table: "event_agenda_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "location_disclosure_audiences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_disclosure_audiences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "location_kinds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_kinds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "location_privacy_erasure_replay_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    authority_sequence = table.Column<long>(type: "bigint", nullable: false),
                    intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_checkpoint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_privacy_erasure_replay_checkpoints", x => x.id);
                    table.CheckConstraint("ck_location_privacy_erasure_replay_checkpoints_chain", "(authority_sequence = 1 AND previous_checkpoint_id IS NULL) OR (authority_sequence > 1 AND previous_checkpoint_id IS NOT NULL)");
                    table.CheckConstraint("ck_location_privacy_erasure_replay_checkpoints_sequence", "authority_sequence > 0");
                    table.ForeignKey(
                        name: "fk_location_privacy_erasure_replay_checkpoints_location_privac",
                        column: x => x.previous_checkpoint_id,
                        principalTable: "location_privacy_erasure_replay_checkpoints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "location_privacy_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    master_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_location_privacy_states", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "location_kinds",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "UNCLASSIFIED", "Unclassified", "Physical location kind has not been reviewed" },
                    { 2, "COMMERCIAL_VENUE", "Commercial venue", "Commercially operated event venue" },
                    { 3, "PUBLIC_SPACE", "Public space", "Publicly accessible physical space" },
                    { 4, "COMMUNITY_VENUE", "Community venue", "Community-operated physical venue" },
                    { 5, "PRIVATE_HOME", "Private home", "Private residential location" }
                });

            migrationBuilder.InsertData(
                table: "location_privacy_states",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "NOT_PROVIDED", "Not provided", "No physical location PII has been provided" },
                    { 2, "ACTIVE", "Active", "Physical location PII is active" },
                    { 3, "ERASED", "Erased", "Physical location PII was irreversibly erased" }
                });

            migrationBuilder.InsertData(
                table: "location_disclosure_audiences",
                columns: new[] { "id", "master_code", "full_name", "description" },
                values: new object[,]
                {
                    { 1, "NEVER", "Never", "Physical location details are never disclosed" },
                    { 2, "ANY_CURRENT_REGISTRANT", "Any current registrant", "Eligible current registrations may receive disclosed details" },
                    { 3, "CONFIRMED_PARTICIPANT", "Confirmed participant", "Only confirmed eligible participants may receive disclosed details" }
                });

            migrationBuilder.CreateTable(
                name: "event_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    show_venue_name = table.Column<bool>(type: "boolean", nullable: false),
                    show_city = table.Column<bool>(type: "boolean", nullable: false),
                    show_country = table.Column<bool>(type: "boolean", nullable: false),
                    show_room_name = table.Column<bool>(type: "boolean", nullable: false),
                    show_street_address = table.Column<bool>(type: "boolean", nullable: false),
                    show_postcode = table.Column<bool>(type: "boolean", nullable: false),
                    show_coordinates = table.Column<bool>(type: "boolean", nullable: false),
                    full_details_audience_id = table.Column<int>(type: "integer", nullable: false),
                    reveal_full_details_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    needs_privacy_review = table.Column<bool>(type: "boolean", nullable: false),
                    is_to_be_announced = table.Column<bool>(type: "boolean", nullable: false),
                    policy_version = table.Column<int>(type: "integer", nullable: false),
                    last_policy_actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_policy_changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_stamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_locations", x => x.id);
                    table.UniqueConstraint("ak_event_locations_tenant_id_event_id_id", x => new { x.tenant_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_locations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_locations_physical_or_tba", "(location_id IS NOT NULL AND is_to_be_announced = false) OR (location_id IS NULL AND is_to_be_announced = true)");
                    table.CheckConstraint("ck_event_locations_policy_version", "policy_version > 0");
                    table.CheckConstraint("ck_event_locations_tba_suppresses_fields", "is_to_be_announced = false OR (show_venue_name = false AND show_city = false AND show_country = false AND show_room_name = false AND show_street_address = false AND show_postcode = false AND show_coordinates = false)");
                    table.ForeignKey(
                        name: "fk_event_locations_events_tenant_id_event_id",
                        columns: x => new { x.tenant_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_locations_location_disclosure_audiences_full_details_",
                        column: x => x.full_details_audience_id,
                        principalTable: "location_disclosure_audiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_locations_locations_tenant_id_location_id",
                        columns: x => new { x.tenant_id, x.location_id },
                        principalTable: "locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_locations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_location_disclosure_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_fields = table.Column<int>(type: "integer", nullable: false),
                    new_fields = table.Column<int>(type: "integer", nullable: false),
                    previous_audience_id = table.Column<int>(type: "integer", nullable: false),
                    new_audience_id = table.Column<int>(type: "integer", nullable: false),
                    previous_reveal_full_details_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    new_reveal_full_details_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    previous_policy_version = table.Column<int>(type: "integer", nullable: false),
                    new_policy_version = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_location_disclosure_audits", x => x.id);
                    table.CheckConstraint("ck_event_location_disclosure_audits_field_flags", "previous_fields BETWEEN 0 AND 127 AND new_fields BETWEEN 0 AND 127");
                    table.CheckConstraint("ck_event_location_disclosure_audits_policy_step", "previous_policy_version >= 0 AND new_policy_version = previous_policy_version + 1");
                    table.CheckConstraint("ck_event_location_disclosure_audits_reason", "reason BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_event_location_disclosure_audits_event_locations_tenant_id_",
                        columns: x => new { x.tenant_id, x.event_location_id },
                        principalTable: "event_locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_location_disclosure_audits_location_disclosure_audien",
                        column: x => x.new_audience_id,
                        principalTable: "location_disclosure_audiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_location_disclosure_audits_location_disclosure_audien1",
                        column: x => x.previous_audience_id,
                        principalTable: "location_disclosure_audiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_location_disclosure_audits_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_location_exact_read_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    was_authorized = table.Column<bool>(type: "boolean", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trace_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_location_exact_read_audits", x => x.id);
                    table.CheckConstraint("ck_event_location_exact_read_audits_purpose", "purpose BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_event_location_exact_read_audits_trace", "correlation_id IS NOT NULL OR trace_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_event_location_exact_read_audits_event_locations_tenant_id_",
                        columns: x => new { x.tenant_id, x.event_location_id },
                        principalTable: "event_locations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_location_exact_read_audits_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_locations_location_kind_id",
                table: "locations",
                column: "location_kind_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_location_privacy_state_id",
                table: "locations",
                column: "location_privacy_state_id");

            migrationBuilder.CreateIndex(
                name: "ix_locations_owner_user_id",
                table: "locations",
                column: "owner_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_erasure_state",
                table: "locations",
                sql: "(location_privacy_state_id = 3 AND owner_user_id IS NULL AND pii_erased_at_utc IS NOT NULL AND pii_erasure_reason IS NOT NULL) OR (location_privacy_state_id <> 3 AND pii_erased_at_utc IS NULL AND pii_erasure_reason IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_locations_owner_private_home",
                table: "locations",
                sql: "owner_user_id IS NULL OR location_kind_id = 5");

            migrationBuilder.CreateIndex(
                name: "ix_location_rooms_location_id",
                table: "location_rooms",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_sessions_elp_consistency",
                table: "event_sessions",
                columns: new[] { "tenant_id", "event_id", "event_location_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_groups_elp_consistency",
                table: "event_session_groups",
                columns: new[] { "tenant_id", "event_id", "event_location_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_agenda_items_elp_consistency",
                table: "event_session_agenda_items",
                columns: new[] { "tenant_id", "event_session_id", "event_location_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_session_agenda_items_tenant_id_event_location_id",
                table: "event_session_agenda_items",
                columns: new[] { "tenant_id", "event_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_agenda_items_elp_consistency",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "event_id", "event_location_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_location_disclosure_audits_history",
                table: "event_location_disclosure_audits",
                columns: new[] { "tenant_id", "event_location_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_event_location_disclosure_audits_new_audience_id",
                table: "event_location_disclosure_audits",
                column: "new_audience_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_location_disclosure_audits_previous_audience_id",
                table: "event_location_disclosure_audits",
                column: "previous_audience_id");

            migrationBuilder.CreateIndex(
                name: "ux_event_location_disclosure_audits_policy_version",
                table: "event_location_disclosure_audits",
                columns: new[] { "tenant_id", "event_location_id", "new_policy_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_location_exact_read_audits_history",
                table: "event_location_exact_read_audits",
                columns: new[] { "tenant_id", "event_location_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_event_location_exact_read_audits_requester",
                table: "event_location_exact_read_audits",
                columns: new[] { "tenant_id", "requester_user_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_event_locations_full_details_audience_id",
                table: "event_locations",
                column: "full_details_audience_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_locations_tenant_event_active",
                table: "event_locations",
                columns: new[] { "tenant_id", "event_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_event_locations_tenant_id_location_id",
                table: "event_locations",
                columns: new[] { "tenant_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ux_event_locations_active_physical",
                table: "event_locations",
                columns: new[] { "tenant_id", "event_id", "location_id" },
                unique: true,
                filter: "is_deleted = false AND is_to_be_announced = false AND location_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_event_locations_active_tba",
                table: "event_locations",
                columns: new[] { "tenant_id", "event_id" },
                unique: true,
                filter: "is_deleted = false AND is_to_be_announced = true");

            migrationBuilder.CreateIndex(
                name: "ix_location_disclosure_audiences_master_code",
                table: "location_disclosure_audiences",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_location_kinds_master_code",
                table: "location_kinds",
                column: "master_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_location_privacy_erasure_checkpoints_intent",
                table: "location_privacy_erasure_replay_checkpoints",
                column: "intent_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_location_privacy_erasure_checkpoints_previous",
                table: "location_privacy_erasure_replay_checkpoints",
                column: "previous_checkpoint_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_location_privacy_erasure_checkpoints_sequence",
                table: "location_privacy_erasure_replay_checkpoints",
                column: "authority_sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_location_privacy_states_master_code",
                table: "location_privacy_states",
                column: "master_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_event_agenda_items_event_locations_tenant_id_event_id_event",
                table: "event_agenda_items",
                columns: new[] { "tenant_id", "event_id", "event_location_id" },
                principalTable: "event_locations",
                principalColumns: new[] { "tenant_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_session_agenda_items_event_locations_tenant_id_event_",
                table: "event_session_agenda_items",
                columns: new[] { "tenant_id", "event_location_id" },
                principalTable: "event_locations",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_session_groups_event_locations_tenant_id_event_id_eve",
                table: "event_session_groups",
                columns: new[] { "tenant_id", "event_id", "event_location_id" },
                principalTable: "event_locations",
                principalColumns: new[] { "tenant_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_event_sessions_event_locations_tenant_id_event_id_event_loc",
                table: "event_sessions",
                columns: new[] { "tenant_id", "event_id", "event_location_id" },
                principalTable: "event_locations",
                principalColumns: new[] { "tenant_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_location_rooms_locations_location_id",
                table: "location_rooms",
                column: "location_id",
                principalTable: "locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_location_kinds_location_kind_id",
                table: "locations",
                column: "location_kind_id",
                principalTable: "location_kinds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_location_privacy_states_location_privacy_state_id",
                table: "locations",
                column: "location_privacy_state_id",
                principalTable: "location_privacy_states",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_locations_users_owner_user_id",
                table: "locations",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_locations_uuid_v7",
                table: "event_locations",
                sql: "substring(id::text, 15, 1) = '7' AND substring(id::text, 20, 1) IN ('8', '9', 'a', 'b')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_location_disclosure_audits_uuid_v7",
                table: "event_location_disclosure_audits",
                sql: "substring(id::text, 15, 1) = '7' AND substring(id::text, 20, 1) IN ('8', '9', 'a', 'b')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_location_exact_read_audits_uuid_v7",
                table: "event_location_exact_read_audits",
                sql: "substring(id::text, 15, 1) = '7' AND substring(id::text, 20, 1) IN ('8', '9', 'a', 'b')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_location_privacy_erasure_checkpoints_uuid_v7",
                table: "location_privacy_erasure_replay_checkpoints",
                sql: "substring(id::text, 15, 1) = '7' AND substring(id::text, 20, 1) IN ('8', '9', 'a', 'b') AND substring(intent_id::text, 15, 1) = '7' AND substring(intent_id::text, 20, 1) IN ('8', '9', 'a', 'b')");

            migrationBuilder.Sql(
                """
                CREATE FUNCTION elp_reject_append_only_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION '% is append-only', TG_TABLE_NAME USING ERRCODE = '55000';
                END;
                $function$;

                CREATE TRIGGER tr_event_location_disclosure_audits_append_only
                BEFORE UPDATE OR DELETE ON event_location_disclosure_audits
                FOR EACH ROW EXECUTE FUNCTION elp_reject_append_only_mutation();

                CREATE TRIGGER tr_event_location_exact_read_audits_append_only
                BEFORE UPDATE OR DELETE ON event_location_exact_read_audits
                FOR EACH ROW EXECUTE FUNCTION elp_reject_append_only_mutation();

                CREATE TRIGGER tr_location_privacy_erasure_checkpoints_append_only
                BEFORE UPDATE OR DELETE ON location_privacy_erasure_replay_checkpoints
                FOR EACH ROW EXECUTE FUNCTION elp_reject_append_only_mutation();

                CREATE FUNCTION elp_validate_carrier_consistency()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    parent_event_id uuid;
                    matches_authority boolean;
                BEGIN
                    IF NEW.event_location_id IS NULL THEN
                        RETURN NEW;
                    END IF;

                    IF TG_TABLE_NAME = 'event_session_agenda_items' THEN
                        SELECT event_id
                        INTO parent_event_id
                        FROM event_sessions
                        WHERE tenant_id = NEW.tenant_id AND id = NEW.event_session_id;
                    ELSE
                        parent_event_id := NEW.event_id;
                    END IF;

                    SELECT EXISTS (
                        SELECT 1
                        FROM event_locations authority
                        WHERE authority.tenant_id = NEW.tenant_id
                          AND authority.event_id = parent_event_id
                          AND authority.id = NEW.event_location_id
                          AND authority.is_deleted = false
                          AND authority.location_id IS NOT DISTINCT FROM NEW.location_id)
                    INTO matches_authority;

                    IF NOT matches_authority THEN
                        RAISE EXCEPTION 'EventLocation carrier does not match tenant, event, active state, or physical location'
                            USING ERRCODE = '23514';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE CONSTRAINT TRIGGER tr_event_sessions_elp_consistency
                AFTER INSERT OR UPDATE ON event_sessions
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION elp_validate_carrier_consistency();

                CREATE CONSTRAINT TRIGGER tr_event_session_groups_elp_consistency
                AFTER INSERT OR UPDATE ON event_session_groups
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION elp_validate_carrier_consistency();

                CREATE CONSTRAINT TRIGGER tr_event_agenda_items_elp_consistency
                AFTER INSERT OR UPDATE ON event_agenda_items
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION elp_validate_carrier_consistency();

                CREATE CONSTRAINT TRIGGER tr_event_session_agenda_items_elp_consistency
                AFTER INSERT OR UPDATE ON event_session_agenda_items
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION elp_validate_carrier_consistency();

                CREATE FUNCTION elp_validate_event_location_references()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF ROW(OLD.tenant_id, OLD.event_id, OLD.location_id, OLD.is_to_be_announced, OLD.is_deleted)
                        IS NOT DISTINCT FROM
                       ROW(NEW.tenant_id, NEW.event_id, NEW.location_id, NEW.is_to_be_announced, NEW.is_deleted) THEN
                        RETURN NEW;
                    END IF;

                    IF EXISTS (SELECT 1 FROM event_sessions WHERE event_location_id = OLD.id)
                       OR EXISTS (SELECT 1 FROM event_session_groups WHERE event_location_id = OLD.id)
                       OR EXISTS (SELECT 1 FROM event_agenda_items WHERE event_location_id = OLD.id)
                       OR EXISTS (SELECT 1 FROM event_session_agenda_items WHERE event_location_id = OLD.id) THEN
                        RAISE EXCEPTION 'Referenced EventLocation identity, physical location, TBA state, or active state is immutable'
                            USING ERRCODE = '23514';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE CONSTRAINT TRIGGER tr_event_locations_reference_consistency
                AFTER UPDATE ON event_locations
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION elp_validate_event_location_references();

                CREATE FUNCTION elp_guard_erased_location_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        IF OLD.location_privacy_state_id = 3 THEN
                            RAISE EXCEPTION 'An erased Location tombstone cannot be deleted' USING ERRCODE = '55000';
                        END IF;
                        RETURN OLD;
                    END IF;

                    IF OLD.location_privacy_state_id = 3
                       AND ROW(OLD.location_privacy_state_id, OLD.location_kind_id, OLD.owner_user_id,
                               OLD.pii_erased_at_utc, OLD.pii_erasure_reason, OLD.full_name, OLD.city)
                           IS DISTINCT FROM
                           ROW(NEW.location_privacy_state_id, NEW.location_kind_id, NEW.owner_user_id,
                               NEW.pii_erased_at_utc, NEW.pii_erasure_reason, NEW.full_name, NEW.city) THEN
                        RAISE EXCEPTION 'An erased Location cannot be changed or resurrected' USING ERRCODE = '55000';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER tr_locations_irreversible_erasure
                BEFORE UPDATE OR DELETE ON locations
                FOR EACH ROW EXECUTE FUNCTION elp_guard_erased_location_mutation();

                CREATE FUNCTION elp_reject_erased_location_pii()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM locations
                        WHERE id = NEW.location_id AND location_privacy_state_id = 3) THEN
                        RAISE EXCEPTION 'PII cannot be attached to an erased Location' USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER tr_location_pii_reject_erased_attach
                BEFORE INSERT OR UPDATE ON location_pii
                FOR EACH ROW EXECUTE FUNCTION elp_reject_erased_location_pii();

                CREATE FUNCTION elp_validate_erased_location_tombstone()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    target_location_id uuid;
                BEGIN
                    IF TG_TABLE_NAME = 'locations' THEN
                        target_location_id := NEW.id;
                    ELSE
                        target_location_id := NEW.location_id;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM locations location
                        WHERE location.id = target_location_id
                          AND location.location_privacy_state_id = 3
                          AND (
                              location.location_kind_id <> 5
                              OR location.owner_user_id IS NOT NULL
                              OR location.pii_erased_at_utc IS NULL
                              OR location.pii_erasure_reason IS NULL
                              OR location.full_name <> 'Private venue'
                              OR location.city <> ''
                              OR EXISTS (SELECT 1 FROM location_pii pii WHERE pii.location_id = location.id)
                              OR EXISTS (
                                  SELECT 1
                                  FROM location_rooms room
                                  WHERE room.location_id = location.id
                                    AND (room.name <> 'privacy-erased-' || replace(room.id::text, '-', '')
                                         OR room.slug IS NOT NULL
                                         OR room.description IS NOT NULL
                                         OR room.is_deleted = false)))) THEN
                        RAISE EXCEPTION 'Erased Location and room tombstones are incomplete' USING ERRCODE = '23514';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE CONSTRAINT TRIGGER tr_locations_validate_erasure_tombstone
                AFTER INSERT OR UPDATE ON locations
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION elp_validate_erased_location_tombstone();

                CREATE CONSTRAINT TRIGGER tr_location_rooms_validate_erasure_tombstone
                AFTER INSERT OR UPDATE ON location_rooms
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION elp_validate_erased_location_tombstone();

                CREATE FUNCTION elp_guard_erased_location_room_delete()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM locations
                        WHERE id = OLD.location_id AND location_privacy_state_id = 3) THEN
                        RAISE EXCEPTION 'A privacy-erased room tombstone cannot be deleted' USING ERRCODE = '55000';
                    END IF;
                    RETURN OLD;
                END;
                $function$;

                CREATE TRIGGER tr_location_rooms_irreversible_tombstone
                BEFORE DELETE ON location_rooms
                FOR EACH ROW EXECUTE FUNCTION elp_guard_erased_location_room_delete();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_location_rooms_irreversible_tombstone ON location_rooms;
                DROP TRIGGER IF EXISTS tr_location_rooms_validate_erasure_tombstone ON location_rooms;
                DROP TRIGGER IF EXISTS tr_locations_validate_erasure_tombstone ON locations;
                DROP TRIGGER IF EXISTS tr_location_pii_reject_erased_attach ON location_pii;
                DROP TRIGGER IF EXISTS tr_locations_irreversible_erasure ON locations;
                DROP TRIGGER IF EXISTS tr_event_locations_reference_consistency ON event_locations;
                DROP TRIGGER IF EXISTS tr_event_session_agenda_items_elp_consistency ON event_session_agenda_items;
                DROP TRIGGER IF EXISTS tr_event_agenda_items_elp_consistency ON event_agenda_items;
                DROP TRIGGER IF EXISTS tr_event_session_groups_elp_consistency ON event_session_groups;
                DROP TRIGGER IF EXISTS tr_event_sessions_elp_consistency ON event_sessions;
                DROP TRIGGER IF EXISTS tr_location_privacy_erasure_checkpoints_append_only ON location_privacy_erasure_replay_checkpoints;
                DROP TRIGGER IF EXISTS tr_event_location_exact_read_audits_append_only ON event_location_exact_read_audits;
                DROP TRIGGER IF EXISTS tr_event_location_disclosure_audits_append_only ON event_location_disclosure_audits;
                DROP FUNCTION IF EXISTS elp_guard_erased_location_room_delete();
                DROP FUNCTION IF EXISTS elp_validate_erased_location_tombstone();
                DROP FUNCTION IF EXISTS elp_reject_erased_location_pii();
                DROP FUNCTION IF EXISTS elp_guard_erased_location_mutation();
                DROP FUNCTION IF EXISTS elp_validate_event_location_references();
                DROP FUNCTION IF EXISTS elp_validate_carrier_consistency();
                DROP FUNCTION IF EXISTS elp_reject_append_only_mutation();
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_event_agenda_items_event_locations_tenant_id_event_id_event",
                table: "event_agenda_items");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_agenda_items_event_locations_tenant_id_event_",
                table: "event_session_agenda_items");

            migrationBuilder.DropForeignKey(
                name: "fk_event_session_groups_event_locations_tenant_id_event_id_eve",
                table: "event_session_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_event_sessions_event_locations_tenant_id_event_id_event_loc",
                table: "event_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_location_rooms_locations_location_id",
                table: "location_rooms");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_location_kinds_location_kind_id",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_location_privacy_states_location_privacy_state_id",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "fk_locations_users_owner_user_id",
                table: "locations");

            migrationBuilder.DropTable(
                name: "event_location_disclosure_audits");

            migrationBuilder.DropTable(
                name: "event_location_exact_read_audits");

            migrationBuilder.DropTable(
                name: "location_kinds");

            migrationBuilder.DropTable(
                name: "location_privacy_erasure_replay_checkpoints");

            migrationBuilder.DropTable(
                name: "location_privacy_states");

            migrationBuilder.DropTable(
                name: "event_locations");

            migrationBuilder.DropTable(
                name: "location_disclosure_audiences");

            migrationBuilder.DropIndex(
                name: "ix_locations_location_kind_id",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_location_privacy_state_id",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_locations_owner_user_id",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_erasure_state",
                table: "locations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_locations_owner_private_home",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "ix_location_rooms_location_id",
                table: "location_rooms");

            migrationBuilder.DropIndex(
                name: "ix_event_sessions_elp_consistency",
                table: "event_sessions");

            migrationBuilder.DropIndex(
                name: "ix_event_session_groups_elp_consistency",
                table: "event_session_groups");

            migrationBuilder.DropIndex(
                name: "ix_event_session_agenda_items_elp_consistency",
                table: "event_session_agenda_items");

            migrationBuilder.DropIndex(
                name: "ix_event_session_agenda_items_tenant_id_event_location_id",
                table: "event_session_agenda_items");

            migrationBuilder.DropIndex(
                name: "ix_event_agenda_items_elp_consistency",
                table: "event_agenda_items");

            migrationBuilder.DropColumn(
                name: "location_kind_id",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "location_privacy_state_id",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "pii_erased_at_utc",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "pii_erasure_reason",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "event_location_id",
                table: "event_sessions");

            migrationBuilder.DropColumn(
                name: "event_location_id",
                table: "event_session_groups");

            migrationBuilder.DropColumn(
                name: "event_location_id",
                table: "event_session_agenda_items");

            migrationBuilder.DropColumn(
                name: "event_location_id",
                table: "event_agenda_items");

            migrationBuilder.CreateIndex(
                name: "ix_event_session_agenda_items_tenant_id_event_session_id",
                table: "event_session_agenda_items",
                columns: new[] { "tenant_id", "event_session_id" });
        }
    }
}
