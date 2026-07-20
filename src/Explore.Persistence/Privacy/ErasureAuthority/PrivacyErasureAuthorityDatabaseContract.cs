// ABOUTME: Defines migration-ready retained-authority schema, role, function, and ACL identifiers.
// ABOUTME: Keeps runtime access function-only and revokes table, sequence, and unsafe default privileges.

namespace Explore.Persistence.Privacy.ErasureAuthority;

public static class PrivacyErasureAuthorityDatabaseContract
{
    public const string SchemaName = "privacy_erasure_authority";
    public const string OwnerRole = "privacy_erasure_authority_owner";
    public const string MigratorRole = "privacy_erasure_authority_migrator";
    public const string RuntimeRole = "privacy_erasure_authority_runtime";
    public const string AppendFunction = "append_erasure_intent";
    public const string ReadFunction = "read_erasure_intents_after";

    public static string AppendFunctionSql =>
        $"{SchemaName}.{AppendFunction}";

    public static string ReadFunctionSql =>
        $"{SchemaName}.{ReadFunction}";

    public static string RoleProvisioningSql { get; } = $"""
        DO $contract$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{OwnerRole}') THEN
                CREATE ROLE {OwnerRole} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            ELSE
                ALTER ROLE {OwnerRole} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            END IF;

            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{MigratorRole}') THEN
                CREATE ROLE {MigratorRole} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            ELSE
                ALTER ROLE {MigratorRole} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            END IF;

            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{RuntimeRole}') THEN
                CREATE ROLE {RuntimeRole} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            ELSE
                ALTER ROLE {RuntimeRole} NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            END IF;
        END
        $contract$;

        GRANT {OwnerRole} TO {MigratorRole};
        """;

    public static string AuthorityObjectsSql { get; } = $"""
        CREATE SCHEMA IF NOT EXISTS {SchemaName};
        ALTER SCHEMA {SchemaName} OWNER TO {OwnerRole};

        ALTER TABLE {SchemaName}.erasure_intents OWNER TO {OwnerRole};
        ALTER TABLE {SchemaName}.authority_counter OWNER TO {OwnerRole};
        ALTER TABLE {SchemaName}.erasure_intents
            ALTER COLUMN requested_at_utc SET DEFAULT statement_timestamp(),
            ALTER COLUMN recorded_at_utc SET DEFAULT clock_timestamp(),
            ALTER COLUMN retention_expires_at_utc
                SET DEFAULT 'infinity'::timestamp with time zone;

        INSERT INTO {SchemaName}.authority_counter (singleton, last_sequence)
        SELECT true, COALESCE(MAX(authority_sequence), 0)
        FROM {SchemaName}.erasure_intents
        ON CONFLICT (singleton) DO UPDATE
        SET last_sequence = GREATEST(
            {SchemaName}.authority_counter.last_sequence,
            EXCLUDED.last_sequence);

        CREATE OR REPLACE FUNCTION {SchemaName}.reject_erasure_intent_mutation()
        RETURNS trigger
        LANGUAGE plpgsql
        SET search_path = pg_catalog, {SchemaName}
        AS $function$
        BEGIN
            RAISE EXCEPTION 'privacy erasure authority facts are immutable'
                USING ERRCODE = '55000';
        END;
        $function$;
        ALTER FUNCTION {SchemaName}.reject_erasure_intent_mutation()
            OWNER TO {OwnerRole};

        DROP TRIGGER IF EXISTS tr_erasure_intents_immutable
            ON {SchemaName}.erasure_intents;
        CREATE TRIGGER tr_erasure_intents_immutable
        BEFORE UPDATE OR DELETE ON {SchemaName}.erasure_intents
        FOR EACH ROW
        EXECUTE FUNCTION {SchemaName}.reject_erasure_intent_mutation();

        DROP TRIGGER IF EXISTS tr_erasure_intents_no_truncate
            ON {SchemaName}.erasure_intents;
        CREATE TRIGGER tr_erasure_intents_no_truncate
        BEFORE TRUNCATE ON {SchemaName}.erasure_intents
        FOR EACH STATEMENT
        EXECUTE FUNCTION {SchemaName}.reject_erasure_intent_mutation();

        CREATE OR REPLACE FUNCTION {SchemaName}.{AppendFunction}(
            p_intent_id uuid,
            p_subject_kind smallint,
            p_subject_id uuid,
            p_reason_code smallint,
            p_policy_version integer)
        RETURNS TABLE
        (
            authority_sequence bigint,
            intent_id uuid,
            subject_kind smallint,
            subject_id uuid,
            reason_code smallint,
            policy_version integer,
            requested_at_utc timestamp with time zone,
            recorded_at_utc timestamp with time zone,
            retention_expires_at_utc timestamp with time zone
        )
        LANGUAGE plpgsql
        SECURITY DEFINER
        SET search_path = pg_catalog, {SchemaName}
        AS $function$
        DECLARE
            v_existing {SchemaName}.erasure_intents%ROWTYPE;
            v_last_sequence bigint;
            v_next_sequence bigint;
        BEGIN
            IF p_intent_id IS NULL
               OR p_intent_id = '00000000-0000-0000-0000-000000000000'::uuid
               OR substring(p_intent_id::text from 15 for 1) <> '7'
               OR substring(p_intent_id::text from 20 for 1) NOT IN ('8', '9', 'a', 'b') THEN
                RAISE EXCEPTION 'IntentId must be an RFC 4122 UUIDv7 value'
                    USING ERRCODE = '22023';
            END IF;

            IF p_subject_kind IS NULL OR p_subject_kind <> 1 THEN
                RAISE EXCEPTION 'Only User privacy erasure is executable'
                    USING ERRCODE = '22023';
            END IF;

            IF p_subject_id IS NULL
               OR p_subject_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
                RAISE EXCEPTION 'SubjectId must be an opaque non-empty identifier'
                    USING ERRCODE = '22023';
            END IF;

            IF p_reason_code IS NULL OR p_reason_code NOT BETWEEN 1 AND 3 THEN
                RAISE EXCEPTION 'ReasonCode must be a defined erasure reason'
                    USING ERRCODE = '22023';
            END IF;

            IF p_policy_version IS NULL OR p_policy_version <= 0 THEN
                RAISE EXCEPTION 'PolicyVersion must be positive'
                    USING ERRCODE = '22023';
            END IF;

            SELECT counter.last_sequence
            INTO v_last_sequence
            FROM {SchemaName}.authority_counter AS counter
            WHERE counter.singleton
            FOR UPDATE;

            IF NOT FOUND THEN
                RAISE EXCEPTION 'Erasure authority counter is unavailable'
                    USING ERRCODE = '55000';
            END IF;

            SELECT retained.*
            INTO v_existing
            FROM {SchemaName}.erasure_intents AS retained
            WHERE retained.intent_id = p_intent_id;

            IF FOUND THEN
                IF v_existing.subject_kind <> p_subject_kind
                   OR v_existing.subject_id <> p_subject_id
                   OR v_existing.reason_code <> p_reason_code
                   OR v_existing.policy_version <> p_policy_version THEN
                    RAISE EXCEPTION 'IntentId is already retained with a different payload'
                        USING ERRCODE = '22023';
                END IF;

                RETURN QUERY
                SELECT retained.authority_sequence,
                       retained.intent_id,
                       retained.subject_kind,
                       retained.subject_id,
                       retained.reason_code,
                       retained.policy_version,
                       retained.requested_at_utc,
                       retained.recorded_at_utc,
                       retained.retention_expires_at_utc
                FROM {SchemaName}.erasure_intents AS retained
                WHERE retained.intent_id = p_intent_id;
                RETURN;
            END IF;

            IF v_last_sequence = 9223372036854775807 THEN
                RAISE EXCEPTION 'Erasure authority sequence is exhausted'
                    USING ERRCODE = '22003';
            END IF;

            v_next_sequence := v_last_sequence + 1;
            UPDATE {SchemaName}.authority_counter AS counter
            SET last_sequence = v_next_sequence
            WHERE counter.singleton;

            RETURN QUERY
            INSERT INTO {SchemaName}.erasure_intents AS retained
                (authority_sequence, intent_id, subject_kind, subject_id, reason_code, policy_version)
            VALUES
                (v_next_sequence, p_intent_id, p_subject_kind, p_subject_id, p_reason_code, p_policy_version)
            RETURNING retained.authority_sequence,
                      retained.intent_id,
                      retained.subject_kind,
                      retained.subject_id,
                      retained.reason_code,
                      retained.policy_version,
                      retained.requested_at_utc,
                      retained.recorded_at_utc,
                      retained.retention_expires_at_utc;
        END;
        $function$;
        ALTER FUNCTION {SchemaName}.{AppendFunction}(uuid, smallint, uuid, smallint, integer)
            OWNER TO {OwnerRole};

        CREATE OR REPLACE FUNCTION {SchemaName}.{ReadFunction}(
            p_authority_sequence bigint,
            p_limit integer)
        RETURNS TABLE
        (
            authority_sequence bigint,
            intent_id uuid,
            subject_kind smallint,
            subject_id uuid,
            reason_code smallint,
            policy_version integer,
            requested_at_utc timestamp with time zone,
            recorded_at_utc timestamp with time zone,
            retention_expires_at_utc timestamp with time zone
        )
        LANGUAGE plpgsql
        STABLE
        SECURITY DEFINER
        SET search_path = pg_catalog, {SchemaName}
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
                   retained.subject_kind,
                   retained.subject_id,
                   retained.reason_code,
                   retained.policy_version,
                   retained.requested_at_utc,
                   retained.recorded_at_utc,
                   retained.retention_expires_at_utc
            FROM {SchemaName}.erasure_intents AS retained
            WHERE retained.authority_sequence > p_authority_sequence
            ORDER BY retained.authority_sequence
            LIMIT p_limit;
        END;
        $function$;
        ALTER FUNCTION {SchemaName}.{ReadFunction}(bigint, integer)
            OWNER TO {OwnerRole};
        """;

    public static string RuntimeAclSql { get; } = $"""
        REVOKE ALL ON SCHEMA {SchemaName} FROM PUBLIC;
        REVOKE ALL ON SCHEMA {SchemaName} FROM {RuntimeRole};
        REVOKE ALL ON ALL TABLES IN SCHEMA {SchemaName} FROM PUBLIC, {RuntimeRole};
        REVOKE ALL ON ALL SEQUENCES IN SCHEMA {SchemaName} FROM PUBLIC, {RuntimeRole};
        REVOKE ALL ON ALL FUNCTIONS IN SCHEMA {SchemaName} FROM PUBLIC, {RuntimeRole};
        ALTER DEFAULT PRIVILEGES FOR ROLE {OwnerRole} IN SCHEMA {SchemaName}
            REVOKE ALL ON TABLES FROM PUBLIC, {RuntimeRole};
        ALTER DEFAULT PRIVILEGES FOR ROLE {OwnerRole} IN SCHEMA {SchemaName}
            REVOKE ALL ON SEQUENCES FROM PUBLIC, {RuntimeRole};
        ALTER DEFAULT PRIVILEGES FOR ROLE {OwnerRole} IN SCHEMA {SchemaName}
            REVOKE ALL ON FUNCTIONS FROM PUBLIC, {RuntimeRole};
        GRANT USAGE ON SCHEMA {SchemaName} TO {RuntimeRole};
        GRANT EXECUTE ON FUNCTION {SchemaName}.{AppendFunction}(uuid, smallint, uuid, smallint, integer)
            TO {RuntimeRole};
        GRANT EXECUTE ON FUNCTION {SchemaName}.{ReadFunction}(bigint, integer)
            TO {RuntimeRole};
        """;

    public static string RoleIsolationSql { get; } = $"""
        DO $contract$
        BEGIN
            IF pg_has_role('{RuntimeRole}', '{OwnerRole}', 'MEMBER')
               OR pg_has_role('{OwnerRole}', '{RuntimeRole}', 'MEMBER')
               OR pg_has_role('{RuntimeRole}', '{MigratorRole}', 'MEMBER')
               OR pg_has_role('{MigratorRole}', '{RuntimeRole}', 'MEMBER') THEN
                RAISE EXCEPTION 'privacy erasure authority runtime role must remain separate';
            END IF;
        END
        $contract$;
        """;

    public static string MigrationSql { get; } = $"""
        {RoleProvisioningSql}
        {AuthorityObjectsSql}
        {RuntimeAclSql}
        {RoleIsolationSql}
        """;
}
