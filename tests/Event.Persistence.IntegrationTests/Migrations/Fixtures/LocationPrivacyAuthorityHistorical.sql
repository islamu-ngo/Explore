-- ABOUTME: Provisions the independently retained PostgreSQL location-erasure authority.
-- ABOUTME: Exposes fixed-search-path security-definer functions while denying runtime table and counter access.

DO $block$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'location_privacy_authority_owner') THEN
        CREATE ROLE location_privacy_authority_owner NOLOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'location_privacy_authority_runtime') THEN
        CREATE ROLE location_privacy_authority_runtime NOLOGIN;
    END IF;
END;
$block$;

CREATE SCHEMA IF NOT EXISTS location_privacy_authority;
ALTER SCHEMA location_privacy_authority OWNER TO location_privacy_authority_owner;

CREATE TABLE IF NOT EXISTS location_privacy_authority.erasure_intents
(
    authority_sequence bigint NOT NULL PRIMARY KEY,
    intent_id uuid NOT NULL UNIQUE,
    owner_user_id uuid NOT NULL,
    location_ids uuid[] NOT NULL,
    reason smallint NOT NULL,
    requested_at_utc timestamp with time zone NOT NULL DEFAULT statement_timestamp(),
    recorded_at_utc timestamp with time zone NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT ck_location_privacy_erasure_intents_intent_uuid_v7
        CHECK (substring(intent_id::text from 15 for 1) = '7'),
    CONSTRAINT ck_location_privacy_erasure_intents_intent_rfc4122_variant
        CHECK (substring(intent_id::text from 20 for 1) IN ('8', '9', 'a', 'b')),
    CONSTRAINT ck_location_privacy_erasure_intents_owner_nonempty
        CHECK (owner_user_id <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT ck_location_privacy_erasure_intents_location_ids_no_empty_uuid
        CHECK (array_position(location_ids, '00000000-0000-0000-0000-000000000000'::uuid) IS NULL),
    CONSTRAINT ck_location_privacy_erasure_intents_location_ids_no_nulls
        CHECK (array_position(location_ids, NULL) IS NULL),
    CONSTRAINT ck_location_privacy_erasure_intents_reason
        CHECK (reason BETWEEN 1 AND 3),
    CONSTRAINT ck_location_privacy_erasure_intents_server_time_order
        CHECK (recorded_at_utc >= requested_at_utc)
);

ALTER TABLE location_privacy_authority.erasure_intents
    ALTER COLUMN authority_sequence DROP IDENTITY IF EXISTS;
ALTER TABLE location_privacy_authority.erasure_intents
    OWNER TO location_privacy_authority_owner;

CREATE TABLE IF NOT EXISTS location_privacy_authority.authority_counter
(
    singleton boolean NOT NULL PRIMARY KEY DEFAULT true,
    last_sequence bigint NOT NULL,
    CONSTRAINT ck_location_privacy_authority_counter_singleton CHECK (singleton),
    CONSTRAINT ck_location_privacy_authority_counter_nonnegative CHECK (last_sequence >= 0)
);
ALTER TABLE location_privacy_authority.authority_counter
    OWNER TO location_privacy_authority_owner;

INSERT INTO location_privacy_authority.authority_counter (singleton, last_sequence)
SELECT true, COALESCE(MAX(authority_sequence), 0)
FROM location_privacy_authority.erasure_intents
ON CONFLICT (singleton) DO UPDATE
SET last_sequence = GREATEST(
    location_privacy_authority.authority_counter.last_sequence,
    EXCLUDED.last_sequence);

CREATE OR REPLACE FUNCTION location_privacy_authority.reject_erasure_intent_mutation()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = pg_catalog, location_privacy_authority
AS $function$
BEGIN
    RAISE EXCEPTION 'location privacy erasure authority facts are immutable'
        USING ERRCODE = '55000';
END;
$function$;
ALTER FUNCTION location_privacy_authority.reject_erasure_intent_mutation()
    OWNER TO location_privacy_authority_owner;

DROP TRIGGER IF EXISTS tr_erasure_intents_immutable
    ON location_privacy_authority.erasure_intents;

CREATE TRIGGER tr_erasure_intents_immutable
BEFORE UPDATE OR DELETE ON location_privacy_authority.erasure_intents
FOR EACH ROW
EXECUTE FUNCTION location_privacy_authority.reject_erasure_intent_mutation();

CREATE OR REPLACE FUNCTION location_privacy_authority.append_erasure_intent(
    p_intent_id uuid,
    p_owner_user_id uuid,
    p_location_ids uuid[],
    p_reason smallint)
RETURNS TABLE
(
    authority_sequence bigint,
    intent_id uuid,
    owner_user_id uuid,
    location_ids uuid[],
    reason smallint,
    requested_at_utc timestamp with time zone,
    recorded_at_utc timestamp with time zone
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, location_privacy_authority
AS $function$
DECLARE
    v_existing location_privacy_authority.erasure_intents%ROWTYPE;
    v_last_sequence bigint;
    v_next_sequence bigint;
    v_location_ids uuid[];
BEGIN
    IF p_intent_id IS NULL
       OR p_intent_id = '00000000-0000-0000-0000-000000000000'::uuid
       OR substring(p_intent_id::text from 15 for 1) <> '7'
       OR substring(p_intent_id::text from 20 for 1) NOT IN ('8', '9', 'a', 'b') THEN
        RAISE EXCEPTION 'IntentId must be an RFC 4122 UUIDv7 value'
            USING ERRCODE = '22023';
    END IF;

    IF p_owner_user_id IS NULL
       OR p_owner_user_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
        RAISE EXCEPTION 'OwnerUserId must be an opaque non-empty identifier'
            USING ERRCODE = '22023';
    END IF;

    IF p_location_ids IS NULL
       OR array_position(p_location_ids, NULL) IS NOT NULL
       OR array_position(
            p_location_ids,
            '00000000-0000-0000-0000-000000000000'::uuid) IS NOT NULL THEN
        RAISE EXCEPTION 'LocationIds must contain only opaque non-empty identifiers'
            USING ERRCODE = '22023';
    END IF;

    IF p_reason IS NULL OR p_reason NOT BETWEEN 1 AND 3 THEN
        RAISE EXCEPTION 'Reason must be a defined erasure reason'
            USING ERRCODE = '22023';
    END IF;

    SELECT COALESCE(array_agg(normalized.location_id ORDER BY normalized.location_id), ARRAY[]::uuid[])
    INTO v_location_ids
    FROM
    (
        SELECT DISTINCT supplied.location_id
        FROM unnest(p_location_ids) AS supplied(location_id)
    ) AS normalized;

    SELECT counter.last_sequence
    INTO v_last_sequence
    FROM location_privacy_authority.authority_counter AS counter
    WHERE counter.singleton
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Erasure authority counter is unavailable'
            USING ERRCODE = '55000';
    END IF;

    SELECT retained.*
    INTO v_existing
    FROM location_privacy_authority.erasure_intents AS retained
    WHERE retained.intent_id = p_intent_id;

    IF FOUND THEN
        IF v_existing.owner_user_id <> p_owner_user_id
           OR v_existing.location_ids <> v_location_ids
           OR v_existing.reason <> p_reason THEN
            RAISE EXCEPTION 'IntentId is already retained with a different normalized payload'
                USING ERRCODE = '22023';
        END IF;

        RETURN QUERY
        SELECT retained.authority_sequence,
               retained.intent_id,
               retained.owner_user_id,
               retained.location_ids,
               retained.reason,
               retained.requested_at_utc,
               retained.recorded_at_utc
        FROM location_privacy_authority.erasure_intents AS retained
        WHERE retained.intent_id = p_intent_id;
        RETURN;
    END IF;

    IF v_last_sequence = 9223372036854775807 THEN
        RAISE EXCEPTION 'Erasure authority sequence is exhausted'
            USING ERRCODE = '22003';
    END IF;

    v_next_sequence := v_last_sequence + 1;
    UPDATE location_privacy_authority.authority_counter AS counter
    SET last_sequence = v_next_sequence
    WHERE counter.singleton;

    RETURN QUERY
    INSERT INTO location_privacy_authority.erasure_intents AS retained
        (authority_sequence, intent_id, owner_user_id, location_ids, reason)
    VALUES
        (v_next_sequence, p_intent_id, p_owner_user_id, v_location_ids, p_reason)
    RETURNING retained.authority_sequence,
              retained.intent_id,
              retained.owner_user_id,
              retained.location_ids,
              retained.reason,
              retained.requested_at_utc,
              retained.recorded_at_utc;
END;
$function$;
ALTER FUNCTION location_privacy_authority.append_erasure_intent(uuid, uuid, uuid[], smallint)
    OWNER TO location_privacy_authority_owner;

CREATE OR REPLACE FUNCTION location_privacy_authority.read_erasure_intents_after(
    p_authority_sequence bigint,
    p_limit integer)
RETURNS TABLE
(
    authority_sequence bigint,
    intent_id uuid,
    owner_user_id uuid,
    location_ids uuid[],
    reason smallint,
    requested_at_utc timestamp with time zone,
    recorded_at_utc timestamp with time zone
)
LANGUAGE plpgsql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, location_privacy_authority
AS $function$
BEGIN
    IF p_authority_sequence IS NULL OR p_authority_sequence < 0 THEN
        RAISE EXCEPTION 'Authority sequence checkpoint cannot be negative'
            USING ERRCODE = '22023';
    END IF;

    IF p_limit IS NULL OR p_limit NOT BETWEEN 1 AND 500 THEN
        RAISE EXCEPTION 'Read limit must be between 1 and 500'
            USING ERRCODE = '22023';
    END IF;

    RETURN QUERY
    SELECT retained.authority_sequence,
           retained.intent_id,
           retained.owner_user_id,
           retained.location_ids,
           retained.reason,
           retained.requested_at_utc,
           retained.recorded_at_utc
    FROM location_privacy_authority.erasure_intents AS retained
    WHERE retained.authority_sequence > p_authority_sequence
    ORDER BY retained.authority_sequence
    LIMIT p_limit;
END;
$function$;
ALTER FUNCTION location_privacy_authority.read_erasure_intents_after(bigint, integer)
    OWNER TO location_privacy_authority_owner;

REVOKE ALL ON SCHEMA location_privacy_authority FROM PUBLIC;
REVOKE ALL ON SCHEMA location_privacy_authority FROM location_privacy_authority_runtime;
REVOKE ALL ON TABLE location_privacy_authority.erasure_intents FROM PUBLIC;
REVOKE ALL ON TABLE location_privacy_authority.erasure_intents FROM location_privacy_authority_runtime;
REVOKE ALL ON TABLE location_privacy_authority.authority_counter FROM PUBLIC;
REVOKE ALL ON TABLE location_privacy_authority.authority_counter FROM location_privacy_authority_runtime;
REVOKE ALL ON FUNCTION location_privacy_authority.reject_erasure_intent_mutation() FROM PUBLIC;
REVOKE ALL ON FUNCTION location_privacy_authority.append_erasure_intent(uuid, uuid, uuid[], smallint) FROM PUBLIC;
REVOKE ALL ON FUNCTION location_privacy_authority.read_erasure_intents_after(bigint, integer) FROM PUBLIC;

GRANT USAGE ON SCHEMA location_privacy_authority TO location_privacy_authority_runtime;
GRANT EXECUTE ON FUNCTION location_privacy_authority.append_erasure_intent(uuid, uuid, uuid[], smallint)
    TO location_privacy_authority_runtime;
GRANT EXECUTE ON FUNCTION location_privacy_authority.read_erasure_intents_after(bigint, integer)
    TO location_privacy_authority_runtime;
