// ABOUTME: Repository-wide ratchet proving obsolete branding-derived identity contracts stay removed.
// ABOUTME: Guards runtime source, configuration, OpenAPI, generated clients, and canonical documentation.

using System.Text;
using Explore.Application.Contracts.Services;

namespace Event.Architecture.Tests;

public sealed class LegalIdentityCutoverArchitectureTests
{
    private static readonly string[] IncludedRoots =
    [
        "docs",
        "schemas",
        "src"
    ];

    private static readonly HashSet<string> TextExtensions =
    [
        ".cs",
        ".css",
        ".env",
        ".example",
        ".json",
        ".md",
        ".razor",
        ".yaml",
        ".yml"
    ];

    [Test]
    public async Task RuntimeGeneratedAndCanonicalDocumentationSurfacesContainNoObsoleteProseContract()
    {
        string[] forbidden =
        [
            "PaidEvent" + "DisclaimerFormatter",
            "PaidEventDirectory" + "Disclaimer",
            "FormatDirectory" + "Disclaimer",
            "provides an event discovery and management " + "directory only"
        ];
        var violations = new List<string>();

        foreach (string rootName in IncludedRoots)
        {
            string root = ContextSystemHelpers.RepoPath(rootName);
            foreach (string path in Directory.EnumerateFiles(
                         root,
                         "*",
                         SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(
                    ContextSystemHelpers.RepoRoot,
                    path);
                if (IsBuildArtifact(relativePath)
                    || !TextExtensions.Contains(Path.GetExtension(path)))
                {
                    continue;
                }

                string content = await File.ReadAllTextAsync(path, Encoding.UTF8);
                foreach (string token in forbidden)
                {
                    if (content.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{relativePath}:{token}");
                    }
                }
            }
        }

        await Assert.That(violations.Distinct(StringComparer.Ordinal).Order()).IsEmpty();
    }

    [Test]
    public async Task CheckoutGovernanceContainsOperationsOnly()
    {
        string[] forbiddenMembers =
        [
            "OperatorId",
            "OperatorDisplayName",
            "IsOfficialInstance",
            "OfficialOrigin",
            "OperatorRegionCode",
            "OperatorWebsiteUrl",
            "OperatorLegalNoticeUrl",
            "OperatorTermsUrl",
            "OperatorPrivacyUrl",
            "ComplaintContact"
        ];

        string[] interfaceMembers = typeof(IPaidCheckoutGovernance)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();
        string apiSettings = await File.ReadAllTextAsync(
            ContextSystemHelpers.RepoPath("src", "Explore.API", "appsettings.json"),
            Encoding.UTF8);
        string standaloneSettings = await File.ReadAllTextAsync(
            ContextSystemHelpers.RepoPath("src", "Event.Standalone", "appsettings.json"),
            Encoding.UTF8);

        foreach (string forbidden in forbiddenMembers)
        {
            await Assert.That(interfaceMembers).DoesNotContain(forbidden);
            await Assert.That(apiSettings).DoesNotContain($"\"{forbidden}\"");
            await Assert.That(standaloneSettings).DoesNotContain($"\"{forbidden}\"");
        }
    }

    private static bool IsBuildArtifact(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar).Any(segment =>
            segment is "bin" or "obj" or "TestResults");
}
