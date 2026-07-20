// ABOUTME: Defines migration-ready retained-authority schema, role, function, and ACL identifiers.
// ABOUTME: Keeps runtime access function-only and revokes table, sequence, and unsafe default privileges.

namespace Explore.Persistence.Privacy.ErasureAuthority;

public static class PrivacyErasureAuthorityDatabaseContract
{
    public const string SchemaName = "privacy_erasure_authority";
    public const string OwnerRole = "privacy_erasure_authority_owner";
    public const string RuntimeRole = "privacy_erasure_authority_runtime";
    public const string AppendFunction = "append_erasure_intent";
    public const string ReadFunction = "read_erasure_intents_after";

    public static string AppendFunctionSql =>
        $"{SchemaName}.{AppendFunction}";

    public static string ReadFunctionSql =>
        $"{SchemaName}.{ReadFunction}";

    public static string RuntimeAclSql { get; } = $"""
        REVOKE ALL ON SCHEMA {SchemaName} FROM PUBLIC;
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
               OR pg_has_role('{OwnerRole}', '{RuntimeRole}', 'MEMBER') THEN
                RAISE EXCEPTION 'privacy erasure authority owner and runtime roles must remain separate';
            END IF;
        END
        $contract$;
        """;
}
