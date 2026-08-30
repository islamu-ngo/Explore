// ABOUTME: Specifies deterministic rendering for the constrained legal Markdown contract.
// ABOUTME: Proves identity substitution is encoded and unsafe or inaccessible shapes fail closed.

namespace Event.Domain.UnitTests.ConfigurationManifest;

using Explore.Domain;

public sealed class LegalMarkdownRenderingInvariantTests
{
    [Test]
    public async Task Render_ProducesDeterministicEncodedAccessibleHtml()
    {
        const string markdown = """
            # Overview

            The accountable operator is {{accountable_identity}}.

            Read [policy details](https://example.test/legal).
            """;
        var identities = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["accountable_identity"] = "Operator & Community"
        };

        LegalMarkdownRenderResult first =
            LegalMarkdownContract.Render(markdown, identities);
        LegalMarkdownRenderResult second =
            LegalMarkdownContract.Render(markdown, identities);

        await Assert.That(first.IsReady).IsTrue();
        await Assert.That(first.Html).IsEqualTo(second.Html);
        await Assert.That(first.Html).IsEqualTo(
            "<h2>Overview</h2>\n"
            + "<p>The accountable operator is Operator &amp; Community.</p>\n"
            + "<p>Read <a href=\"https://example.test/legal\" rel=\"noopener noreferrer\">policy details</a>.</p>\n");
        await Assert.That(first.LinkTargets)
            .IsEquivalentTo(["https://example.test/legal"]);
        await Assert.That(first.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task Render_UnresolvedIdentityReturnsNoPublicHtml()
    {
        LegalMarkdownRenderResult result = LegalMarkdownContract.Render(
            "# Policy\n\nAccountable operator: {{accountable_identity}}.",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(result.Html).IsEmpty();
        await Assert.That(result.Diagnostics.Select(item => item.Code))
            .Contains("legal_markdown_identity_unresolved");
    }

    [Test]
    public async Task Inspect_RejectsHeadingJumpsAndUnsupportedExecutableShapes()
    {
        await Assert.That(() => LegalMarkdownContract.Inspect(
                "# Policy\n\n### Skipped heading"))
            .Throws<ArgumentException>();
        await Assert.That(() => LegalMarkdownContract.Inspect(
                "# Policy\n\n```javascript\nalert(1)\n```"))
            .Throws<ArgumentException>();
        await Assert.That(() => LegalMarkdownContract.Inspect(
                "[remote](https://example.test/policy?tracking=1)"))
            .Throws<ArgumentException>();
        await Assert.That(() => LegalMarkdownContract.Inspect(
                "[local](https://127.0.0.1/legal)"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Render_ReportsWeakLinkTextWithoutLeakingDestination()
    {
        LegalMarkdownRenderResult result = LegalMarkdownContract.Render(
            "# Policy\n\n[click here](https://example.test/legal)",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await Assert.That(result.IsReady).IsTrue();
        LegalMarkdownDiagnostic diagnostic = result.Diagnostics.Single();
        await Assert.That(diagnostic.Code)
            .IsEqualTo("legal_markdown_link_text_weak");
        await Assert.That(diagnostic.Subject).IsNull();
    }
}
