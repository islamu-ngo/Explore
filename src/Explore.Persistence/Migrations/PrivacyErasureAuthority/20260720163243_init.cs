using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Explore.Persistence.Migrations.PrivacyErasureAuthority
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "privacy_erasure_authority");

            migrationBuilder.CreateTable(
                name: "authority_counter",
                schema: "privacy_erasure_authority",
                columns: table => new
                {
                    singleton = table.Column<bool>(type: "boolean", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authority_counter", x => x.singleton);
                    table.CheckConstraint("ck_privacy_erasure_authority_counter_nonnegative", "last_sequence >= 0");
                    table.CheckConstraint("ck_privacy_erasure_authority_counter_singleton", "singleton");
                });

            migrationBuilder.CreateTable(
                name: "erasure_intents",
                schema: "privacy_erasure_authority",
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
                    retention_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "'infinity'::timestamp with time zone")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_erasure_intents", x => x.authority_sequence);
                    table.UniqueConstraint("ak_erasure_intents_intent_id", x => x.intent_id);
                    table.CheckConstraint("ck_privacy_erasure_intents_intent_rfc4122_variant", "substring(intent_id::text, 20, 1) IN ('8', '9', 'a', 'b')");
                    table.CheckConstraint("ck_privacy_erasure_intents_intent_uuid_v7", "substring(intent_id::text, 15, 1) = '7'");
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
                schema: "privacy_erasure_authority",
                table: "erasure_intents",
                columns: new[] { "intent_id", "subject_kind", "policy_version" },
                unique: true);

            migrationBuilder.Sql(
                """
                DO $roles$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_catalog.pg_roles
                        WHERE rolname = 'privacy_erasure_authority_owner') THEN
                        CREATE ROLE privacy_erasure_authority_owner
                            NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
                            NOINHERIT NOREPLICATION NOBYPASSRLS;
                    ELSE
                        ALTER ROLE privacy_erasure_authority_owner
                            NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
                            NOINHERIT NOREPLICATION NOBYPASSRLS;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_catalog.pg_roles
                        WHERE rolname = 'privacy_erasure_authority_migrator') THEN
                        CREATE ROLE privacy_erasure_authority_migrator
                            NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
                            NOINHERIT NOREPLICATION NOBYPASSRLS;
                    ELSE
                        ALTER ROLE privacy_erasure_authority_migrator
                            NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
                            NOINHERIT NOREPLICATION NOBYPASSRLS;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_catalog.pg_roles
                        WHERE rolname = 'privacy_erasure_authority_runtime') THEN
                        CREATE ROLE privacy_erasure_authority_runtime
                            NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
                            NOINHERIT NOREPLICATION NOBYPASSRLS;
                    ELSE
                        ALTER ROLE privacy_erasure_authority_runtime
                            NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
                            NOINHERIT NOREPLICATION NOBYPASSRLS;
                    END IF;
                END
                $roles$;

                GRANT privacy_erasure_authority_owner
                    TO privacy_erasure_authority_migrator;

                ALTER SCHEMA privacy_erasure_authority
                    OWNER TO privacy_erasure_authority_owner;
                ALTER TABLE privacy_erasure_authority.erasure_intents
                    OWNER TO privacy_erasure_authority_owner;
                ALTER TABLE privacy_erasure_authority.authority_counter
                    OWNER TO privacy_erasure_authority_owner;

                ALTER TABLE privacy_erasure_authority.erasure_intents
                    ALTER COLUMN requested_at_utc SET DEFAULT statement_timestamp(),
                    ALTER COLUMN recorded_at_utc SET DEFAULT clock_timestamp(),
                    ALTER COLUMN retention_expires_at_utc
                        SET DEFAULT 'infinity'::timestamp with time zone;

                INSERT INTO privacy_erasure_authority.authority_counter
                    (singleton, last_sequence)
                VALUES (TRUE, 0)
                ON CONFLICT (singleton) DO NOTHING;

                CREATE OR REPLACE FUNCTION
                    privacy_erasure_authority.reject_erasure_intent_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog, privacy_erasure_authority
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'privacy erasure authority facts are immutable'
                        USING ERRCODE = '55000';
                END
                $function$;
                ALTER FUNCTION
                    privacy_erasure_authority.reject_erasure_intent_mutation()
                    OWNER TO privacy_erasure_authority_owner;

                CREATE TRIGGER tr_erasure_intents_immutable
                BEFORE UPDATE OR DELETE
                ON privacy_erasure_authority.erasure_intents
                FOR EACH ROW
                EXECUTE FUNCTION
                    privacy_erasure_authority.reject_erasure_intent_mutation();

                CREATE TRIGGER tr_erasure_intents_no_truncate
                BEFORE TRUNCATE
                ON privacy_erasure_authority.erasure_intents
                FOR EACH STATEMENT
                EXECUTE FUNCTION
                    privacy_erasure_authority.reject_erasure_intent_mutation();

                CREATE OR REPLACE FUNCTION
                    privacy_erasure_authority.append_erasure_intent(
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
                SET search_path = pg_catalog, privacy_erasure_authority
                AS $function$
                DECLARE
                    existing_intent privacy_erasure_authority.erasure_intents%ROWTYPE;
                    current_sequence bigint;
                    next_sequence bigint;
                BEGIN
                    IF p_intent_id IS NULL
                       OR p_intent_id = '00000000-0000-0000-0000-000000000000'::uuid
                       OR substring(p_intent_id::text from 15 for 1) <> '7'
                       OR substring(p_intent_id::text from 20 for 1)
                            NOT IN ('8', '9', 'a', 'b') THEN
                        RAISE EXCEPTION 'IntentId must be an RFC 4122 UUIDv7 value'
                            USING ERRCODE = '22023';
                    END IF;

                    IF p_subject_kind IS NULL OR p_subject_kind <> 1 THEN
                        RAISE EXCEPTION 'Only User privacy erasure is executable'
                            USING ERRCODE = '22023';
                    END IF;

                    IF p_subject_id IS NULL
                       OR p_subject_id =
                            '00000000-0000-0000-0000-000000000000'::uuid THEN
                        RAISE EXCEPTION 'SubjectId must be a non-empty opaque identifier'
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
                    INTO current_sequence
                    FROM privacy_erasure_authority.authority_counter AS counter
                    WHERE counter.singleton
                    FOR UPDATE;

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'Erasure authority counter is unavailable'
                            USING ERRCODE = '55000';
                    END IF;

                    SELECT retained.*
                    INTO existing_intent
                    FROM privacy_erasure_authority.erasure_intents AS retained
                    WHERE retained.intent_id = p_intent_id;

                    IF FOUND THEN
                        IF existing_intent.subject_kind <> p_subject_kind
                           OR existing_intent.subject_id <> p_subject_id
                           OR existing_intent.reason_code <> p_reason_code
                           OR existing_intent.policy_version <> p_policy_version THEN
                            RAISE EXCEPTION
                                'IntentId is already retained with a different payload'
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
                        FROM privacy_erasure_authority.erasure_intents AS retained
                        WHERE retained.intent_id = p_intent_id;
                        RETURN;
                    END IF;

                    IF current_sequence = 9223372036854775807 THEN
                        RAISE EXCEPTION 'Erasure authority sequence is exhausted'
                            USING ERRCODE = '22003';
                    END IF;

                    next_sequence := current_sequence + 1;
                    UPDATE privacy_erasure_authority.authority_counter AS counter
                    SET last_sequence = next_sequence
                    WHERE counter.singleton;

                    RETURN QUERY
                    INSERT INTO privacy_erasure_authority.erasure_intents AS retained
                        (authority_sequence, intent_id, subject_kind, subject_id,
                         reason_code, policy_version)
                    VALUES
                        (next_sequence, p_intent_id, p_subject_kind, p_subject_id,
                         p_reason_code, p_policy_version)
                    RETURNING retained.authority_sequence,
                              retained.intent_id,
                              retained.subject_kind,
                              retained.subject_id,
                              retained.reason_code,
                              retained.policy_version,
                              retained.requested_at_utc,
                              retained.recorded_at_utc,
                              retained.retention_expires_at_utc;
                END
                $function$;
                ALTER FUNCTION
                    privacy_erasure_authority.append_erasure_intent(
                        uuid, smallint, uuid, smallint, integer)
                    OWNER TO privacy_erasure_authority_owner;

                CREATE OR REPLACE FUNCTION
                    privacy_erasure_authority.read_erasure_intents_after(
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
                SET search_path = pg_catalog, privacy_erasure_authority
                AS $function$
                BEGIN
                    IF p_authority_sequence IS NULL OR p_authority_sequence < 0 THEN
                        RAISE EXCEPTION
                            'Authority sequence checkpoint cannot be negative'
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
                    FROM privacy_erasure_authority.erasure_intents AS retained
                    WHERE retained.authority_sequence > p_authority_sequence
                    ORDER BY retained.authority_sequence
                    LIMIT p_limit;
                END
                $function$;
                ALTER FUNCTION
                    privacy_erasure_authority.read_erasure_intents_after(
                        bigint, integer)
                    OWNER TO privacy_erasure_authority_owner;

                DO $acl_preflight$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_class AS relation_entry
                        JOIN pg_catalog.pg_namespace AS schema_entry
                          ON schema_entry.oid = relation_entry.relnamespace
                        CROSS JOIN LATERAL pg_catalog.aclexplode(
                            COALESCE(
                                relation_entry.relacl,
                                pg_catalog.acldefault(
                                    CASE relation_entry.relkind
                                        WHEN 'S' THEN 'S'::"char"
                                        ELSE 'r'::"char"
                                    END,
                                    relation_entry.relowner))) AS privilege_entry
                        LEFT JOIN pg_catalog.pg_roles AS grantee_role
                          ON grantee_role.oid = privilege_entry.grantee
                        WHERE schema_entry.nspname = 'privacy_erasure_authority'
                          AND privilege_entry.grantee <> 0
                          AND grantee_role.rolname NOT IN (
                              'privacy_erasure_authority_owner',
                              'privacy_erasure_authority_runtime'))
                       OR EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_proc AS function_entry
                        JOIN pg_catalog.pg_namespace AS schema_entry
                          ON schema_entry.oid = function_entry.pronamespace
                        CROSS JOIN LATERAL pg_catalog.aclexplode(
                            COALESCE(
                                function_entry.proacl,
                                pg_catalog.acldefault(
                                    'f', function_entry.proowner))) AS privilege_entry
                        LEFT JOIN pg_catalog.pg_roles AS grantee_role
                          ON grantee_role.oid = privilege_entry.grantee
                        WHERE schema_entry.nspname = 'privacy_erasure_authority'
                          AND privilege_entry.grantee <> 0
                          AND grantee_role.rolname NOT IN (
                              'privacy_erasure_authority_owner',
                              'privacy_erasure_authority_runtime'))
                       OR EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_default_acl AS default_acl
                        JOIN pg_catalog.pg_roles AS owner_role
                          ON owner_role.oid = default_acl.defaclrole
                        LEFT JOIN pg_catalog.pg_namespace AS schema_entry
                          ON schema_entry.oid = default_acl.defaclnamespace
                        CROSS JOIN LATERAL pg_catalog.aclexplode(
                            default_acl.defaclacl) AS privilege_entry
                        LEFT JOIN pg_catalog.pg_roles AS grantee_role
                          ON grantee_role.oid = privilege_entry.grantee
                        WHERE owner_role.rolname =
                                'privacy_erasure_authority_owner'
                          AND schema_entry.nspname =
                                'privacy_erasure_authority'
                          AND privilege_entry.grantee <> 0
                          AND grantee_role.rolname NOT IN (
                              'privacy_erasure_authority_owner',
                              'privacy_erasure_authority_runtime')) THEN
                        RAISE EXCEPTION
                            'privacy erasure authority contains an unrelated ACL grantee'
                            USING ERRCODE = '55000';
                    END IF;
                END
                $acl_preflight$;

                REVOKE ALL ON SCHEMA privacy_erasure_authority FROM PUBLIC;
                REVOKE ALL ON SCHEMA privacy_erasure_authority
                    FROM privacy_erasure_authority_runtime;
                REVOKE ALL ON ALL TABLES IN SCHEMA privacy_erasure_authority
                    FROM PUBLIC, privacy_erasure_authority_runtime;
                REVOKE ALL ON ALL SEQUENCES IN SCHEMA privacy_erasure_authority
                    FROM PUBLIC, privacy_erasure_authority_runtime;
                REVOKE ALL ON ALL FUNCTIONS IN SCHEMA privacy_erasure_authority
                    FROM PUBLIC, privacy_erasure_authority_runtime;

                ALTER DEFAULT PRIVILEGES
                    FOR ROLE privacy_erasure_authority_owner
                    IN SCHEMA privacy_erasure_authority
                    REVOKE ALL ON TABLES
                    FROM PUBLIC, privacy_erasure_authority_runtime;
                ALTER DEFAULT PRIVILEGES
                    FOR ROLE privacy_erasure_authority_owner
                    IN SCHEMA privacy_erasure_authority
                    REVOKE ALL ON SEQUENCES
                    FROM PUBLIC, privacy_erasure_authority_runtime;
                ALTER DEFAULT PRIVILEGES
                    FOR ROLE privacy_erasure_authority_owner
                    IN SCHEMA privacy_erasure_authority
                    REVOKE ALL ON FUNCTIONS
                    FROM PUBLIC, privacy_erasure_authority_runtime;

                GRANT USAGE ON SCHEMA privacy_erasure_authority
                    TO privacy_erasure_authority_runtime;
                GRANT EXECUTE ON FUNCTION
                    privacy_erasure_authority.append_erasure_intent(
                        uuid, smallint, uuid, smallint, integer)
                    TO privacy_erasure_authority_runtime;
                GRANT EXECUTE ON FUNCTION
                    privacy_erasure_authority.read_erasure_intents_after(
                        bigint, integer)
                    TO privacy_erasure_authority_runtime;

                DO $role_isolation$
                BEGIN
                    IF pg_has_role(
                            'privacy_erasure_authority_runtime',
                            'privacy_erasure_authority_owner',
                            'MEMBER')
                       OR pg_has_role(
                            'privacy_erasure_authority_owner',
                            'privacy_erasure_authority_runtime',
                            'MEMBER')
                       OR pg_has_role(
                            'privacy_erasure_authority_runtime',
                            'privacy_erasure_authority_migrator',
                            'MEMBER')
                       OR pg_has_role(
                            'privacy_erasure_authority_migrator',
                            'privacy_erasure_authority_runtime',
                            'MEMBER') THEN
                        RAISE EXCEPTION
                            'privacy erasure authority runtime role must remain separate'
                            USING ERRCODE = '55000';
                    END IF;
                END
                $role_isolation$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $rollback$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM privacy_erasure_authority.erasure_intents
                        UNION ALL
                        SELECT 1
                        FROM privacy_erasure_authority.authority_counter
                        WHERE last_sequence <> 0) THEN
                        RAISE EXCEPTION
                            'retained privacy evidence prevents authority rollback'
                            USING ERRCODE = '55000';
                    END IF;
                END
                $rollback$;

                DROP FUNCTION IF EXISTS
                    privacy_erasure_authority.append_erasure_intent(
                        uuid, smallint, uuid, smallint, integer);
                DROP FUNCTION IF EXISTS
                    privacy_erasure_authority.read_erasure_intents_after(
                        bigint, integer);
                DROP FUNCTION IF EXISTS
                    privacy_erasure_authority.reject_erasure_intent_mutation()
                    CASCADE;
                """);

            migrationBuilder.DropTable(
                name: "authority_counter",
                schema: "privacy_erasure_authority");

            migrationBuilder.DropTable(
                name: "erasure_intents",
                schema: "privacy_erasure_authority");

            migrationBuilder.Sql(
                "DROP SCHEMA IF EXISTS privacy_erasure_authority;");
        }
    }
}
