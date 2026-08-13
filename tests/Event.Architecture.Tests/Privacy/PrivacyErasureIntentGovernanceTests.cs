// ABOUTME: Guards the privacy-erasure intent's authority-first topology and ownership contract.
// ABOUTME: Rejects stale behavior modes, incomplete scope, and weakened safety prohibitions.

namespace Event.Architecture.Tests.Privacy;

public sealed class PrivacyErasureIntentGovernanceTests
{
    private static readonly string[] RequiredContractTerms =
    [
        "one authority-first workflow",
        "PrivacyErasure:Authority:Topology",
        "EmbeddedSqlite",
        "ExternalDatabase",
        "CoLocated",
        "default is EmbeddedSqlite",
        "application-side replay checkpoint remains active in all topologies",
        "A present legacy PrivacyErasure:Durability:Mode key is rejected",
        "reset-only",
        "receipt/status",
        "provider settlement",
        "startup replay",
        "retention",
        "restore",
        "For ExternalDatabase, API runtime and MigrationService receive separate runtime and migrator authority credentials",
        "mapped to structured privacy-prefixed database fields only in the owning process",
        "EmbeddedSqlite and CoLocated receive no authority database credential and Blazor receives neither secret",
        "CoLocated reports restoreReplayProtection=false and is backed up and restored atomically with the primary database",
        "dev/active/optional-retained-erasure-authority/**",
        ".omo/evidence/optional-retained-erasure-authority/**",
        "docs/DEPLOYMENT_MODES.md",
        "docs/DEPLOYMENT_TIERS.md",
        "Adding arbitrary JSON, table, column, SQL, reflection-driven, prompt-derived, or other executable erasure instructions",
        "Storing live PII in the authority database, joining it from normal request paths, or introducing a distributed transaction",
        "Bypassing tenant filters outside the one dedicated Persistence erasure adapter with a named reason and exact subject and tenant predicates",
        "Calling Keycloak, ATProto, Listmonk, SMTP, object storage, webhooks, or any provider inside a handler, database transaction, migration, or startup replay transaction",
        "Emitting PII, linkable identifiers, secrets, receipt credentials, provider payloads/responses, URLs, exception text, or unbounded values",
        "Deleting any database, container, volume, backup, unrelated file, or unrelated dirty-worktree change",
        "Weakening or deleting failing tests, tenant isolation, least-privilege ACLs, append-only guards, irreversible guards, legal holds, retention checks, receipt authorization, or HAL affordance rules"
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
            "For ExternalDatabase, API runtime and MigrationService receive separate runtime and migrator authority credentials mapped to structured privacy-prefixed database fields only in the owning process; EmbeddedSqlite and CoLocated receive no authority database credential and Blazor receives neither secret";
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
