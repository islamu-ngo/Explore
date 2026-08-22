// ABOUTME: Guards the privacy-erasure intent's authority-first topology and ownership contract.
// ABOUTME: Rejects stale behavior modes, incomplete scope, and weakened safety prohibitions.

namespace Event.Architecture.Tests.Privacy;

public sealed class PrivacyErasureIntentGovernanceTests
{
    private static readonly string[] RequiredContractTerms =
    [
        "PrivacyErasure:Authority:Topology",
        "EmbeddedSqlite",
        "ExternalDatabase",
        "CoLocated",
        "Primary application and Data Protection persistence retain PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL",
        "Unsupported SQL Server, MariaDB, and MySQL CoLocated authority combinations fail during composition before adapter use and without exposing credentials, connection strings, or structured database values",
        "Provider-native authority adapters remain distinct",
        "MigrationService applies exactly one authority migration path",
        "Hand-editing generated migration, designer, or model-snapshot artifacts",
        "Weakening tenant filters, named query filters, subject predicates, or exact tenant predicates",
        "Collapsing PostgreSQL, SQLite, and external PostgreSQL provider-native authority adapters into one generic authority implementation",
        "Adding provider fallback, backward-compatibility translation, dual-write, or compatibility shims for unsupported topology/provider combinations",
        "Exposing secrets, credentials, generated connection strings, structured database values, provider payloads, or raw exception text",
        "Expanding CoLocated authority support beyond PostgreSQL and SQLite",
        "Collapsing application, Data Protection, embedded authority, co-located authority, or external authority migration ownership into one project, schema, or history table"
    ];

    private static readonly string[] StaleAcceptanceMandates =
    [
        "PrivacyErasure:Durability:Mode accepts only ApplicationDatabase or RetainedAuthority",
        "ApplicationDatabase is the default",
        "RetainedAuthority startup migrates",
        "PrivacyErasure__Durability__Mode",
        "The application-database and retained-authority restore paths prove",
        "authority-free local-lite",
        "CoLocated proves rollback replay",
        "ConnectionStrings__PrivacyErasureAuthority"
    ];

    [Test]
    public async Task PlatformPrivacyErasureIntent_OwnsAuthorityFirstTopologyContract()
    {
        string catalog = await File.ReadAllTextAsync(ContextSystemHelpers.RepoPath(
            ".agents", "contract", "intents.yaml"));

        RequireAuthorityFirstContract(SelectIntent(catalog, "platform-privacy-erasure"));
    }

    [Test]
    [Arguments("PrivacyErasure:Authority:Topology")]
    [Arguments("EmbeddedSqlite")]
    [Arguments("ExternalDatabase")]
    [Arguments("CoLocated")]
    public async Task AuthorityFirstContract_RejectsMissingTopologyTerm(string requiredTerm)
    {
        string catalog = await File.ReadAllTextAsync(ContextSystemHelpers.RepoPath(
            ".agents", "contract", "intents.yaml"));
        string intent = SelectIntent(catalog, "platform-privacy-erasure")
            .Replace(requiredTerm, string.Empty, StringComparison.Ordinal);

        await Assert.That(() => RequireAuthorityFirstContract(intent))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AuthorityFirstContract_RejectsMissingCredentialSeparationAcceptance()
    {
        const string credentialAcceptance =
            "Unsupported SQL Server, MariaDB, and MySQL CoLocated authority combinations fail during composition before adapter use and without exposing credentials, connection strings, or structured database values";
        string catalog = await File.ReadAllTextAsync(ContextSystemHelpers.RepoPath(
            ".agents", "contract", "intents.yaml"));
        string intent = SelectIntent(catalog, "platform-privacy-erasure");
        string mutatedIntent = intent.Replace(credentialAcceptance, string.Empty, StringComparison.Ordinal);

        await Assert.That(mutatedIntent).IsNotEqualTo(intent);
        await Assert.That(() => RequireAuthorityFirstContract(mutatedIntent))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task IntentSelector_RejectsMissingOrMalformedIntent()
    {
        const string malformedCatalog = "intents:\n  - id platform-privacy-erasure\n";

        await Assert.That(() => SelectIntent(malformedCatalog, "platform-privacy-erasure"))
            .Throws<InvalidOperationException>();
    }

    private static string SelectIntent(string catalog, string intentId)
    {
        string marker = $"  - id: {intentId}";
        int start = catalog.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Intent '{intentId}' was not found.");
        }

        int end = catalog.IndexOf("\n  - id: ", start + marker.Length, StringComparison.Ordinal);
        return end < 0 ? catalog[start..] : catalog[start..end];
    }

    private static void RequireAuthorityFirstContract(string intent)
    {
        string[] missing = RequiredContractTerms
            .Where(term => !intent.Contains(term, StringComparison.Ordinal))
            .ToArray();
        string[] stale = StaleAcceptanceMandates
            .Where(term => intent.Contains(term, StringComparison.Ordinal))
            .ToArray();

        if (missing.Length > 0 || stale.Length > 0)
        {
            throw new InvalidOperationException(
                $"Missing terms: {string.Join(", ", missing)}; stale mandates: {string.Join(", ", stale)}");
        }
    }
}
