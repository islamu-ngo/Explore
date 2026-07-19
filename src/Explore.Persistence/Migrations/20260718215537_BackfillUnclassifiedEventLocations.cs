// ABOUTME: Backfills legacy Location lifecycle and event-local physical placement with fail-closed privacy defaults.
// ABOUTME: Keeps the operator-selected stage repeat-safe, auditable, measurable, and reversible before contract activation.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUnclassifiedEventLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS event_location_privacy_backfill_reversal (
                    location_id uuid PRIMARY KEY,
                    previous_privacy_state_id integer NOT NULL,
                    backfilled_privacy_state_id integer NOT NULL,
                    recorded_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT ck_elp_backfill_reversal_state_ids
                        CHECK (previous_privacy_state_id IN (1, 2)
                               AND backfilled_privacy_state_id IN (1, 2)
                               AND previous_privacy_state_id <> backfilled_privacy_state_id)
                );

                CREATE TABLE IF NOT EXISTS event_location_privacy_carrier_backfill_reversal (
                    carrier_table text NOT NULL,
                    carrier_id uuid NOT NULL,
                    event_location_id uuid NOT NULL,
                    previous_location_id uuid NULL,
                    backfilled_location_id uuid NOT NULL,
                    recorded_at_utc timestamp with time zone NOT NULL,
                    PRIMARY KEY (carrier_table, carrier_id),
                    CONSTRAINT ck_elp_carrier_backfill_reversal_table
                        CHECK (carrier_table IN (
                            'event_sessions',
                            'event_session_groups',
                            'event_agenda_items',
                            'event_session_agenda_items'))
                );

                INSERT INTO event_location_privacy_backfill_reversal (
                    location_id,
                    previous_privacy_state_id,
                    backfilled_privacy_state_id,
                    recorded_at_utc)
                SELECT
                    location.id,
                    location.location_privacy_state_id,
                    CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM location_pii pii
                            WHERE pii.location_id = location.id)
                        THEN 2
                        ELSE 1
                    END,
                    CURRENT_TIMESTAMP
                FROM locations location
                WHERE location.location_privacy_state_id <> 3
                  AND location.location_privacy_state_id <> CASE
                      WHEN EXISTS (
                          SELECT 1
                          FROM location_pii pii
                          WHERE pii.location_id = location.id)
                      THEN 2
                      ELSE 1
                  END
                ON CONFLICT (location_id) DO NOTHING;

                UPDATE locations location
                SET location_privacy_state_id = reversal.backfilled_privacy_state_id
                FROM event_location_privacy_backfill_reversal reversal
                WHERE reversal.location_id = location.id
                  AND location.location_privacy_state_id = reversal.previous_privacy_state_id;

                DO $block$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM locations location
                        WHERE location.location_privacy_state_id <> 3
                          AND location.location_privacy_state_id <> CASE
                              WHEN EXISTS (
                                  SELECT 1
                                  FROM location_pii pii
                                  WHERE pii.location_id = location.id)
                              THEN 2
                              ELSE 1
                          END) THEN
                        RAISE EXCEPTION 'ELP-230B could not deterministically backfill every non-erased Location privacy state'
                            USING ERRCODE = '23514';
                    END IF;
                END;
                $block$;

                CREATE TEMP TABLE elp_backfill_pairs (
                    tenant_id uuid NOT NULL,
                    event_id uuid NOT NULL,
                    location_id uuid NOT NULL,
                    source_actor_user_id uuid NULL,
                    PRIMARY KEY (tenant_id, event_id, location_id)
                ) ON COMMIT DROP;

                WITH carrier_sources AS (
                    SELECT
                        session.tenant_id,
                        session.event_id,
                        COALESCE(session.location_id, room.location_id) AS location_id,
                        session.created_by AS source_actor_user_id
                    FROM event_sessions session
                    LEFT JOIN location_rooms room
                      ON room.tenant_id = session.tenant_id
                     AND room.id = session.room_id
                    WHERE COALESCE(session.location_id, room.location_id) IS NOT NULL

                    UNION ALL

                    SELECT
                        session_group.tenant_id,
                        session_group.event_id,
                        COALESCE(session_group.location_id, room.location_id) AS location_id,
                        session_group.created_by AS source_actor_user_id
                    FROM event_session_groups session_group
                    LEFT JOIN location_rooms room
                      ON room.tenant_id = session_group.tenant_id
                     AND room.id = session_group.room_id
                    WHERE COALESCE(session_group.location_id, room.location_id) IS NOT NULL

                    UNION ALL

                    SELECT
                        agenda_item.tenant_id,
                        agenda_item.event_id,
                        COALESCE(agenda_item.location_id, room.location_id) AS location_id,
                        agenda_item.created_by AS source_actor_user_id
                    FROM event_agenda_items agenda_item
                    LEFT JOIN location_rooms room
                      ON room.tenant_id = agenda_item.tenant_id
                     AND room.id = agenda_item.room_id
                    WHERE COALESCE(agenda_item.location_id, room.location_id) IS NOT NULL

                    UNION ALL

                    SELECT
                        session_agenda_item.tenant_id,
                        parent_session.event_id,
                        session_agenda_item.location_id,
                        parent_session.created_by AS source_actor_user_id
                    FROM event_session_agenda_items session_agenda_item
                    INNER JOIN event_sessions parent_session
                      ON parent_session.tenant_id = session_agenda_item.tenant_id
                     AND parent_session.id = session_agenda_item.event_session_id
                    WHERE session_agenda_item.location_id IS NOT NULL
                )
                INSERT INTO elp_backfill_pairs (
                    tenant_id,
                    event_id,
                    location_id,
                    source_actor_user_id)
                SELECT
                    source.tenant_id,
                    source.event_id,
                    source.location_id,
                    COALESCE(event.created_by, MIN(source.source_actor_user_id::text)::uuid)
                FROM carrier_sources source
                INNER JOIN events event
                  ON event.tenant_id = source.tenant_id
                 AND event.id = source.event_id
                GROUP BY
                    source.tenant_id,
                    source.event_id,
                    source.location_id,
                    event.created_by;

                DO $block$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM elp_backfill_pairs pair
                        LEFT JOIN locations location
                          ON location.tenant_id = pair.tenant_id
                         AND location.id = pair.location_id
                        WHERE location.id IS NULL) THEN
                        RAISE EXCEPTION 'ELP-230B found a physical carrier whose Location is missing or belongs to another tenant'
                            USING ERRCODE = '23503';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM elp_backfill_pairs pair
                        WHERE (pair.source_actor_user_id IS NULL
                               OR pair.source_actor_user_id = '00000000-0000-0000-0000-000000000000'::uuid)
                          AND NOT EXISTS (
                              SELECT 1
                              FROM event_locations authority
                              WHERE authority.tenant_id = pair.tenant_id
                                AND authority.event_id = pair.event_id
                                AND authority.location_id = pair.location_id
                                AND authority.is_to_be_announced = false
                                AND authority.is_deleted = false)) THEN
                        RAISE EXCEPTION 'ELP-230B cannot create truthful legacy policy audit evidence without a source user id'
                            USING ERRCODE = '23514';
                    END IF;
                END;
                $block$;

                CREATE TEMP TABLE elp_inserted_event_locations (
                    id uuid PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    event_id uuid NOT NULL,
                    location_id uuid NOT NULL,
                    source_actor_user_id uuid NOT NULL
                ) ON COMMIT DROP;

                WITH inserted AS (
                    INSERT INTO event_locations (
                        id,
                        tenant_id,
                        event_id,
                        location_id,
                        show_venue_name,
                        show_city,
                        show_country,
                        show_room_name,
                        show_street_address,
                        show_postcode,
                        show_coordinates,
                        full_details_audience_id,
                        reveal_full_details_from_utc,
                        needs_privacy_review,
                        is_to_be_announced,
                        policy_version,
                        last_policy_actor_user_id,
                        last_policy_changed_at_utc,
                        created_at,
                        created_by,
                        updated_at,
                        updated_by,
                        is_deleted,
                        deleted_at,
                        deleted_by,
                        concurrency_stamp)
                    SELECT
                        uuidv7(),
                        pair.tenant_id,
                        pair.event_id,
                        pair.location_id,
                        false,
                        false,
                        true,
                        false,
                        false,
                        false,
                        false,
                        1,
                        NULL,
                        true,
                        false,
                        1,
                        pair.source_actor_user_id,
                        CURRENT_TIMESTAMP,
                        CURRENT_TIMESTAMP,
                        pair.source_actor_user_id,
                        NULL,
                        NULL,
                        false,
                        NULL,
                        NULL,
                        uuidv7()
                    FROM elp_backfill_pairs pair
                    WHERE pair.source_actor_user_id IS NOT NULL
                      AND NOT EXISTS (
                          SELECT 1
                          FROM event_locations authority
                          WHERE authority.tenant_id = pair.tenant_id
                            AND authority.event_id = pair.event_id
                            AND authority.location_id = pair.location_id
                            AND authority.is_to_be_announced = false
                            AND authority.is_deleted = false)
                    ON CONFLICT DO NOTHING
                    RETURNING id, tenant_id, event_id, location_id, last_policy_actor_user_id
                )
                INSERT INTO elp_inserted_event_locations (
                    id,
                    tenant_id,
                    event_id,
                    location_id,
                    source_actor_user_id)
                SELECT
                    inserted.id,
                    inserted.tenant_id,
                    inserted.event_id,
                    inserted.location_id,
                    inserted.last_policy_actor_user_id
                FROM inserted;

                INSERT INTO event_location_disclosure_audits (
                    id,
                    tenant_id,
                    event_location_id,
                    actor_user_id,
                    previous_fields,
                    new_fields,
                    previous_audience_id,
                    new_audience_id,
                    previous_reveal_full_details_from_utc,
                    new_reveal_full_details_from_utc,
                    previous_policy_version,
                    new_policy_version,
                    reason,
                    occurred_at_utc)
                SELECT
                    uuidv7(),
                    inserted.tenant_id,
                    inserted.id,
                    inserted.source_actor_user_id,
                    0,
                    4,
                    1,
                    1,
                    NULL,
                    NULL,
                    0,
                    1,
                    5,
                    CURRENT_TIMESTAMP
                FROM elp_inserted_event_locations inserted;

                INSERT INTO event_location_privacy_carrier_backfill_reversal (
                    carrier_table,
                    carrier_id,
                    event_location_id,
                    previous_location_id,
                    backfilled_location_id,
                    recorded_at_utc)
                SELECT
                    'event_sessions',
                    carrier.id,
                    authority.id,
                    carrier.location_id,
                    pair.location_id,
                    CURRENT_TIMESTAMP
                FROM event_sessions carrier
                INNER JOIN elp_backfill_pairs pair
                  ON pair.tenant_id = carrier.tenant_id
                 AND pair.event_id = carrier.event_id
                INNER JOIN event_locations authority
                  ON authority.tenant_id = pair.tenant_id
                 AND authority.event_id = pair.event_id
                 AND authority.location_id = pair.location_id
                 AND authority.is_to_be_announced = false
                 AND authority.is_deleted = false
                WHERE carrier.event_location_id IS NULL
                  AND (
                      carrier.location_id = pair.location_id
                      OR EXISTS (
                          SELECT 1
                          FROM location_rooms room
                          WHERE room.tenant_id = carrier.tenant_id
                            AND room.id = carrier.room_id
                            AND room.location_id = pair.location_id))
                ON CONFLICT (carrier_table, carrier_id) DO NOTHING;

                UPDATE event_sessions carrier
                SET location_id = reversal.backfilled_location_id,
                    event_location_id = reversal.event_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_sessions'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id IS NULL;

                INSERT INTO event_location_privacy_carrier_backfill_reversal (
                    carrier_table,
                    carrier_id,
                    event_location_id,
                    previous_location_id,
                    backfilled_location_id,
                    recorded_at_utc)
                SELECT
                    'event_session_groups',
                    carrier.id,
                    authority.id,
                    carrier.location_id,
                    pair.location_id,
                    CURRENT_TIMESTAMP
                FROM event_session_groups carrier
                INNER JOIN elp_backfill_pairs pair
                  ON pair.tenant_id = carrier.tenant_id
                 AND pair.event_id = carrier.event_id
                INNER JOIN event_locations authority
                  ON authority.tenant_id = pair.tenant_id
                 AND authority.event_id = pair.event_id
                 AND authority.location_id = pair.location_id
                 AND authority.is_to_be_announced = false
                 AND authority.is_deleted = false
                WHERE carrier.event_location_id IS NULL
                  AND (
                      carrier.location_id = pair.location_id
                      OR EXISTS (
                          SELECT 1
                          FROM location_rooms room
                          WHERE room.tenant_id = carrier.tenant_id
                            AND room.id = carrier.room_id
                            AND room.location_id = pair.location_id))
                ON CONFLICT (carrier_table, carrier_id) DO NOTHING;

                UPDATE event_session_groups carrier
                SET location_id = reversal.backfilled_location_id,
                    event_location_id = reversal.event_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_session_groups'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id IS NULL;

                INSERT INTO event_location_privacy_carrier_backfill_reversal (
                    carrier_table,
                    carrier_id,
                    event_location_id,
                    previous_location_id,
                    backfilled_location_id,
                    recorded_at_utc)
                SELECT
                    'event_agenda_items',
                    carrier.id,
                    authority.id,
                    carrier.location_id,
                    pair.location_id,
                    CURRENT_TIMESTAMP
                FROM event_agenda_items carrier
                INNER JOIN elp_backfill_pairs pair
                  ON pair.tenant_id = carrier.tenant_id
                 AND pair.event_id = carrier.event_id
                INNER JOIN event_locations authority
                  ON authority.tenant_id = pair.tenant_id
                 AND authority.event_id = pair.event_id
                 AND authority.location_id = pair.location_id
                 AND authority.is_to_be_announced = false
                 AND authority.is_deleted = false
                WHERE carrier.event_location_id IS NULL
                  AND (
                      carrier.location_id = pair.location_id
                      OR EXISTS (
                          SELECT 1
                          FROM location_rooms room
                          WHERE room.tenant_id = carrier.tenant_id
                            AND room.id = carrier.room_id
                            AND room.location_id = pair.location_id))
                ON CONFLICT (carrier_table, carrier_id) DO NOTHING;

                UPDATE event_agenda_items carrier
                SET location_id = reversal.backfilled_location_id,
                    event_location_id = reversal.event_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_agenda_items'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id IS NULL;

                INSERT INTO event_location_privacy_carrier_backfill_reversal (
                    carrier_table,
                    carrier_id,
                    event_location_id,
                    previous_location_id,
                    backfilled_location_id,
                    recorded_at_utc)
                SELECT
                    'event_session_agenda_items',
                    carrier.id,
                    authority.id,
                    carrier.location_id,
                    carrier.location_id,
                    CURRENT_TIMESTAMP
                FROM event_session_agenda_items carrier
                INNER JOIN event_sessions parent_session
                  ON parent_session.tenant_id = carrier.tenant_id
                 AND parent_session.id = carrier.event_session_id
                INNER JOIN event_locations authority
                  ON authority.tenant_id = parent_session.tenant_id
                 AND authority.event_id = parent_session.event_id
                 AND authority.location_id IS NOT NULL
                 AND authority.is_to_be_announced = false
                 AND authority.is_deleted = false
                WHERE carrier.location_id = authority.location_id
                  AND carrier.event_location_id IS NULL
                ON CONFLICT (carrier_table, carrier_id) DO NOTHING;

                UPDATE event_session_agenda_items carrier
                SET event_location_id = reversal.event_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_session_agenda_items'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id IS NULL;

                DO $block$
                DECLARE
                    unclassified_locations bigint;
                    unresolved_event_locations bigint;
                    unresolved_physical_carriers bigint;
                    unresolved_null_location_carriers bigint;
                BEGIN
                    SELECT COUNT(*)
                    INTO unresolved_physical_carriers
                    FROM (
                        SELECT 1 FROM event_sessions WHERE location_id IS NOT NULL AND event_location_id IS NULL
                        UNION ALL
                        SELECT 1 FROM event_session_groups WHERE location_id IS NOT NULL AND event_location_id IS NULL
                        UNION ALL
                        SELECT 1 FROM event_agenda_items WHERE location_id IS NOT NULL AND event_location_id IS NULL
                        UNION ALL
                        SELECT 1 FROM event_session_agenda_items WHERE location_id IS NOT NULL AND event_location_id IS NULL
                    ) unresolved;

                    IF unresolved_physical_carriers <> 0 THEN
                        RAISE EXCEPTION 'ELP-230B left % physical carriers without EventLocation authority',
                            unresolved_physical_carriers
                            USING ERRCODE = '23514';
                    END IF;

                    SELECT COUNT(*)
                    INTO unclassified_locations
                    FROM locations
                    WHERE location_kind_id = 1;

                    SELECT COUNT(*)
                    INTO unresolved_event_locations
                    FROM event_locations
                    WHERE needs_privacy_review = true
                      AND is_deleted = false;

                    SELECT COUNT(*)
                    INTO unresolved_null_location_carriers
                    FROM (
                        SELECT 1 FROM event_sessions WHERE location_id IS NULL AND event_location_id IS NULL
                        UNION ALL
                        SELECT 1 FROM event_session_groups WHERE location_id IS NULL AND event_location_id IS NULL
                        UNION ALL
                        SELECT 1 FROM event_agenda_items WHERE location_id IS NULL AND event_location_id IS NULL
                        UNION ALL
                        SELECT 1 FROM event_session_agenda_items WHERE location_id IS NULL AND event_location_id IS NULL
                    ) unresolved;

                    RAISE NOTICE 'ELP-230B review metrics: unclassified_locations=%, needs_privacy_review_event_locations=%, unresolved_null_location_carriers=%',
                        unclassified_locations,
                        unresolved_event_locations,
                        unresolved_null_location_carriers;
                END;
                $block$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $block$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM event_location_disclosure_audits legacy_audit
                        INNER JOIN event_locations authority
                          ON authority.tenant_id = legacy_audit.tenant_id
                         AND authority.id = legacy_audit.event_location_id
                        WHERE legacy_audit.reason = 5
                          AND legacy_audit.previous_policy_version = 0
                          AND legacy_audit.new_policy_version = 1
                          AND (
                              authority.policy_version <> 1
                              OR authority.show_venue_name = true
                              OR authority.show_city = true
                              OR authority.show_country = false
                              OR authority.show_room_name = true
                              OR authority.show_street_address = true
                              OR authority.show_postcode = true
                              OR authority.show_coordinates = true
                              OR authority.full_details_audience_id <> 1
                              OR authority.reveal_full_details_from_utc IS NOT NULL
                              OR authority.needs_privacy_review = false
                              OR authority.is_to_be_announced = true
                              OR authority.is_deleted = true
                              OR (
                                  SELECT COUNT(*)
                                  FROM event_location_disclosure_audits audit
                                  WHERE audit.tenant_id = authority.tenant_id
                                    AND audit.event_location_id = authority.id) <> 1
                              OR EXISTS (
                                  SELECT 1
                                  FROM event_location_exact_read_audits exact_read
                                  WHERE exact_read.tenant_id = authority.tenant_id
                                    AND exact_read.event_location_id = authority.id))) THEN
                        RAISE EXCEPTION 'ELP-230B Down cannot discard EventLocations changed after the legacy backfill'
                            USING ERRCODE = '55000';
                    END IF;

                    IF to_regclass('event_location_privacy_backfill_reversal') IS NULL THEN
                        RAISE EXCEPTION 'ELP-230B reversal ledger is missing'
                            USING ERRCODE = '55000';
                    END IF;

                    IF to_regclass('event_location_privacy_carrier_backfill_reversal') IS NULL THEN
                        RAISE EXCEPTION 'ELP-230B carrier reversal ledger is missing'
                            USING ERRCODE = '55000';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM event_location_privacy_backfill_reversal reversal
                        LEFT JOIN locations location ON location.id = reversal.location_id
                        WHERE location.id IS NULL
                           OR location.location_privacy_state_id <> reversal.backfilled_privacy_state_id) THEN
                        RAISE EXCEPTION 'ELP-230B Down cannot overwrite a Location privacy state changed after backfill'
                            USING ERRCODE = '55000';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM event_location_privacy_carrier_backfill_reversal reversal
                        LEFT JOIN event_sessions carrier
                          ON reversal.carrier_table = 'event_sessions'
                         AND carrier.id = reversal.carrier_id
                        WHERE reversal.carrier_table = 'event_sessions'
                          AND (carrier.id IS NULL
                               OR carrier.event_location_id IS DISTINCT FROM reversal.event_location_id
                               OR carrier.location_id IS DISTINCT FROM reversal.backfilled_location_id))
                    OR EXISTS (
                        SELECT 1
                        FROM event_location_privacy_carrier_backfill_reversal reversal
                        LEFT JOIN event_session_groups carrier
                          ON reversal.carrier_table = 'event_session_groups'
                         AND carrier.id = reversal.carrier_id
                        WHERE reversal.carrier_table = 'event_session_groups'
                          AND (carrier.id IS NULL
                               OR carrier.event_location_id IS DISTINCT FROM reversal.event_location_id
                               OR carrier.location_id IS DISTINCT FROM reversal.backfilled_location_id))
                    OR EXISTS (
                        SELECT 1
                        FROM event_location_privacy_carrier_backfill_reversal reversal
                        LEFT JOIN event_agenda_items carrier
                          ON reversal.carrier_table = 'event_agenda_items'
                         AND carrier.id = reversal.carrier_id
                        WHERE reversal.carrier_table = 'event_agenda_items'
                          AND (carrier.id IS NULL
                               OR carrier.event_location_id IS DISTINCT FROM reversal.event_location_id
                               OR carrier.location_id IS DISTINCT FROM reversal.backfilled_location_id))
                    OR EXISTS (
                        SELECT 1
                        FROM event_location_privacy_carrier_backfill_reversal reversal
                        LEFT JOIN event_session_agenda_items carrier
                          ON reversal.carrier_table = 'event_session_agenda_items'
                         AND carrier.id = reversal.carrier_id
                        WHERE reversal.carrier_table = 'event_session_agenda_items'
                          AND (carrier.id IS NULL
                               OR carrier.event_location_id IS DISTINCT FROM reversal.event_location_id
                               OR carrier.location_id IS DISTINCT FROM reversal.backfilled_location_id)) THEN
                        RAISE EXCEPTION 'ELP-230B Down cannot overwrite a carrier changed after backfill'
                            USING ERRCODE = '55000';
                    END IF;

                    IF EXISTS (
                        WITH backfilled_authorities AS (
                            SELECT legacy_audit.event_location_id
                            FROM event_location_disclosure_audits legacy_audit
                            WHERE legacy_audit.reason = 5
                              AND legacy_audit.previous_policy_version = 0
                              AND legacy_audit.new_policy_version = 1
                        ), carrier_references AS (
                            SELECT 'event_sessions'::text AS carrier_table, id AS carrier_id, event_location_id
                            FROM event_sessions WHERE event_location_id IS NOT NULL
                            UNION ALL
                            SELECT 'event_session_groups', id, event_location_id
                            FROM event_session_groups WHERE event_location_id IS NOT NULL
                            UNION ALL
                            SELECT 'event_agenda_items', id, event_location_id
                            FROM event_agenda_items WHERE event_location_id IS NOT NULL
                            UNION ALL
                            SELECT 'event_session_agenda_items', id, event_location_id
                            FROM event_session_agenda_items WHERE event_location_id IS NOT NULL
                        )
                        SELECT 1
                        FROM carrier_references carrier
                        INNER JOIN backfilled_authorities authority
                          ON authority.event_location_id = carrier.event_location_id
                        LEFT JOIN event_location_privacy_carrier_backfill_reversal reversal
                          ON reversal.carrier_table = carrier.carrier_table
                         AND reversal.carrier_id = carrier.carrier_id
                         AND reversal.event_location_id = carrier.event_location_id
                        WHERE reversal.carrier_id IS NULL) THEN
                        RAISE EXCEPTION 'ELP-230B Down cannot delete an EventLocation referenced after backfill'
                            USING ERRCODE = '55000';
                    END IF;
                END;
                $block$;

                CREATE TEMP TABLE elp_down_event_location_ids (
                    id uuid PRIMARY KEY
                ) ON COMMIT DROP;

                INSERT INTO elp_down_event_location_ids (id)
                SELECT legacy_audit.event_location_id
                FROM event_location_disclosure_audits legacy_audit
                WHERE legacy_audit.reason = 5
                  AND legacy_audit.previous_policy_version = 0
                  AND legacy_audit.new_policy_version = 1;

                UPDATE event_sessions carrier
                SET event_location_id = NULL,
                    location_id = reversal.previous_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_sessions'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id = reversal.event_location_id;

                UPDATE event_session_groups carrier
                SET event_location_id = NULL,
                    location_id = reversal.previous_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_session_groups'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id = reversal.event_location_id;

                UPDATE event_agenda_items carrier
                SET event_location_id = NULL,
                    location_id = reversal.previous_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_agenda_items'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id = reversal.event_location_id;

                UPDATE event_session_agenda_items carrier
                SET event_location_id = NULL,
                    location_id = reversal.previous_location_id
                FROM event_location_privacy_carrier_backfill_reversal reversal
                WHERE reversal.carrier_table = 'event_session_agenda_items'
                  AND reversal.carrier_id = carrier.id
                  AND carrier.event_location_id = reversal.event_location_id;

                ALTER TABLE event_location_disclosure_audits
                    DISABLE TRIGGER tr_event_location_disclosure_audits_append_only;

                DELETE FROM event_location_disclosure_audits
                WHERE event_location_id IN (SELECT id FROM elp_down_event_location_ids);

                ALTER TABLE event_location_disclosure_audits
                    ENABLE TRIGGER tr_event_location_disclosure_audits_append_only;

                DELETE FROM event_locations
                WHERE id IN (SELECT id FROM elp_down_event_location_ids);

                UPDATE locations location
                SET location_privacy_state_id = reversal.previous_privacy_state_id
                FROM event_location_privacy_backfill_reversal reversal
                WHERE reversal.location_id = location.id;

                DROP TABLE event_location_privacy_carrier_backfill_reversal;
                DROP TABLE event_location_privacy_backfill_reversal;
                """);
        }
    }
}
