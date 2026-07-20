// ABOUTME: Verifies the retained privacy-erasure SQL contract is complete, callable, and least-privileged.
// ABOUTME: Correlates active function definitions, repository signatures, ownership, ACLs, and append-only guards.

using System.Reflection;
using System.Text.RegularExpressions;
using Explore.Persistence.Privacy.ErasureAuthority;

namespace Event.Persistence.IntegrationTests.Privacy;

public sealed partial class PrivacyErasureAuthorityDatabaseContractTests
{
    private const string Schema = "privacy_erasure_authority";
    private const string Owner = "privacy_erasure_authority_owner";
    private const string Migrator = "privacy_erasure_authority_migrator";
    private const string Runtime = "privacy_erasure_authority_runtime";

    [Test]
    public async Task Roles_AreExplicitlyProvisionedWithOnlyMigratorAllowedToAssumeOwner()
    {
        string sql = ContractSql();

        await Assert.That(sql).Contains($"CREATE ROLE {Owner} NOLOGIN");
        await Assert.That(sql).Contains($"CREATE ROLE {Migrator} NOLOGIN");
        await Assert.That(sql).Contains($"CREATE ROLE {Runtime} NOLOGIN");
        await Assert.That(sql).Contains($"GRANT {Owner} TO {Migrator}");
        await Assert.That(sql).DoesNotContain($"GRANT {Owner} TO {Runtime}");
        await Assert.That(sql).DoesNotContain($"GRANT {Migrator} TO {Runtime}");
        await Assert.That(sql).Contains($"pg_has_role('{Runtime}', '{Owner}', 'MEMBER')");
        await Assert.That(sql).Contains($"pg_has_role('{Runtime}', '{Migrator}', 'MEMBER')");
    }

    [Test]
    public async Task RuntimeGrants_TargetExactlyOneActiveDefinitionPerRepositorySignature()
    {
        string sql = ContractSql();
        string[] expected =
        [
            $"{Schema}.append_erasure_intent(uuid,smallint,uuid,smallint,integer)",
            $"{Schema}.read_erasure_intents_after(bigint,integer)"
        ];

        string[] definitions = FunctionDefinitionRegex()
            .Matches(sql)
            .Select(match => NormalizeDefinition(match))
            .Where(signature => expected.Contains(signature, StringComparer.Ordinal))
            .ToArray();
        string[] grants = FunctionGrantRegex()
            .Matches(sql)
            .Select(match => NormalizeGrant(match))
            .ToArray();

        await Assert.That(definitions).IsEquivalentTo(expected);
        await Assert.That(grants).IsEquivalentTo(expected);
        await Assert.That(definitions.Length).IsEqualTo(definitions.Distinct().Count());
    }

    [Test]
    public async Task RepositoryFunctions_AreSecurityDefinerWithFixedSearchPathAndExactRows()
    {
        string sql = ContractSql();
        const string expectedColumns = """
            authority_sequence bigint,
                intent_id uuid,
                subject_kind smallint,
                subject_id uuid,
                reason_code smallint,
                policy_version integer,
                requested_at_utc timestamp with time zone,
                recorded_at_utc timestamp with time zone,
                retention_expires_at_utc timestamp with time zone
            """;

        foreach (string function in new[] { "append_erasure_intent", "read_erasure_intents_after" })
        {
            Match definition = FunctionBodyRegex(function).Match(sql);
            await Assert.That(definition.Success).IsTrue();
            await Assert.That(definition.Value).Contains("SECURITY DEFINER");
            await Assert.That(definition.Value)
                .Contains($"SET search_path = pg_catalog, {Schema}");
            await Assert.That(NormalizeWhitespace(definition.Groups["returns"].Value))
                .IsEqualTo(NormalizeWhitespace(expectedColumns));
        }
    }

    [Test]
    public async Task OwnershipAndAcl_KeepRuntimeFunctionOnlyAndDefaultsClosed()
    {
        string sql = ContractSql();

        await Assert.That(sql).Contains($"ALTER SCHEMA {Schema} OWNER TO {Owner}");
        await Assert.That(sql).Contains($"ALTER TABLE {Schema}.erasure_intents OWNER TO {Owner}");
        await Assert.That(sql).Contains($"ALTER TABLE {Schema}.authority_counter OWNER TO {Owner}");
        await Assert.That(sql).Contains($"ALTER FUNCTION {Schema}.append_erasure_intent(uuid, smallint, uuid, smallint, integer)");
        await Assert.That(sql).Contains($"ALTER FUNCTION {Schema}.read_erasure_intents_after(bigint, integer)");
        await Assert.That(sql).Contains($"REVOKE ALL ON ALL TABLES IN SCHEMA {Schema} FROM PUBLIC, {Runtime}");
        await Assert.That(sql).Contains($"REVOKE ALL ON ALL SEQUENCES IN SCHEMA {Schema} FROM PUBLIC, {Runtime}");
        await Assert.That(sql).Contains($"REVOKE ALL ON ALL FUNCTIONS IN SCHEMA {Schema} FROM PUBLIC, {Runtime}");
        await Assert.That(sql).Contains($"ALTER DEFAULT PRIVILEGES FOR ROLE {Owner} IN SCHEMA {Schema}");
        await Assert.That(sql).DoesNotContain($"GRANT SELECT ON");
        await Assert.That(sql).DoesNotContain($"GRANT INSERT ON");
        await Assert.That(sql).DoesNotContain($"GRANT UPDATE ON");
        await Assert.That(sql).DoesNotContain($"GRANT DELETE ON");
        await Assert.That(sql).DoesNotContain($"GRANT TRUNCATE ON");
    }

    [Test]
    public async Task AppendReadAndTriggers_EnforceTypedIdempotentMonotonicAppendOnlyBehavior()
    {
        string sql = ContractSql();

        await Assert.That(sql).Contains("p_subject_kind <> 1");
        await Assert.That(sql).Contains("p_subject_id = '00000000-0000-0000-0000-000000000000'::uuid");
        await Assert.That(sql).Contains("p_reason_code NOT BETWEEN 1 AND 3");
        await Assert.That(sql).Contains("p_policy_version <= 0");
        await Assert.That(sql).Contains("FOR UPDATE");
        await Assert.That(sql).Contains("v_existing.subject_kind <> p_subject_kind");
        await Assert.That(sql).Contains("v_existing.subject_id <> p_subject_id");
        await Assert.That(sql).Contains("v_existing.reason_code <> p_reason_code");
        await Assert.That(sql).Contains("v_existing.policy_version <> p_policy_version");
        await Assert.That(sql).Contains("v_last_sequence = 9223372036854775807");
        await Assert.That(sql).Contains("SET last_sequence = v_next_sequence");
        await Assert.That(sql).Contains("p_limit NOT BETWEEN 1 AND 500");
        await Assert.That(sql).Contains("ORDER BY retained.authority_sequence");
        await Assert.That(sql).Contains("BEFORE UPDATE OR DELETE ON");
        await Assert.That(sql).Contains("BEFORE TRUNCATE ON");
        await Assert.That(sql).Contains("RAISE EXCEPTION 'privacy erasure authority facts are immutable'");
    }

    private static string ContractSql() =>
        (string?)typeof(PrivacyErasureAuthorityDatabaseContract)
            .GetProperty("MigrationSql", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null)
        ?? string.Join(
                Environment.NewLine,
                typeof(PrivacyErasureAuthorityDatabaseContract)
                    .GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(property => property.PropertyType == typeof(string))
                    .Select(property => (string?)property.GetValue(null))
                    .Where(value => value is not null));

    private static string NormalizeDefinition(Match match)
    {
        string arguments = string.Join(
            ",",
            match.Groups["arguments"].Value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(argument => argument.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1]));
        return $"{match.Groups["name"].Value}({arguments})";
    }

    private static string NormalizeGrant(Match match) =>
        $"{match.Groups["name"].Value}({Regex.Replace(match.Groups["arguments"].Value, @"\s+", string.Empty)})";

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ");

    [GeneratedRegex(
        @"CREATE OR REPLACE FUNCTION\s+(?<name>[\w.]+)\s*\((?<arguments>[^)]*)\)",
        RegexOptions.IgnoreCase)]
    private static partial Regex FunctionDefinitionRegex();

    [GeneratedRegex(
        @"GRANT EXECUTE ON FUNCTION\s+(?<name>[\w.]+)\s*\((?<arguments>[^)]*)\)",
        RegexOptions.IgnoreCase)]
    private static partial Regex FunctionGrantRegex();

    private static Regex FunctionBodyRegex(string function) =>
        new(
            $@"CREATE OR REPLACE FUNCTION\s+{Schema}\.{function}\s*\([^)]*\)\s*" +
            @"RETURNS TABLE\s*\((?<returns>[^)]*)\).*?\$function\$;",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
}
