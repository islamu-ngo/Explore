// ABOUTME: Enforces repository documentation metadata, source anchors, and stale command guardrails.
// ABOUTME: Starts with newly canonical docs so legacy documentation can migrate without noisy failures.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;
using static ContextSystemHelpers;

public class DocumentationQualityTests
{
    private static readonly string[] RequiredMetadataDocs =
    [
        RepoPath("docs", "DOCUMENTATION_ARCHITECTURE.md"),
        RepoPath("docs", "DOCUMENTATION_STYLE_GUIDE.md"),
        RepoPath("docs", "index.md"),
        RepoPath("docs", "API.md"),
        RepoPath("docs", "BLAZOR.md"),
        RepoPath("docs", "CONFIGURATION.md"),
        RepoPath("docs", "SECURITY.md"),
        RepoPath("docs", "FEDERATION.md"),
        RepoPath("docs", "ACCESSIBILITY_ARTIFACTS.md"),
        RepoPath("docs", "PUBLIC_DOCS_ROADMAP.md"),
        RepoPath("docs", "API_COOKBOOK.md"),
        RepoPath("docs", "ADMIN_GUIDE.md"),
        RepoPath("docs", "STORAGE.md"),
        RepoPath("docs", "EMAIL_NOTIFICATIONS.md"),
        RepoPath("docs", "TEMPLATE_SYNC.md"),
        RepoPath("docs", "CONTACT_SHARING.md"),
        RepoPath("docs", "NOTIFICATIONS.md"),
        RepoPath("docs", "SEO.md"),
        RepoPath("docs", "BENCHMARKS.md"),
        RepoPath("docs", "FIRST_CONTRIBUTION.md"),
        RepoPath("docs", "GETTING_STARTED.md"),
        RepoPath("docs", "CONTRIBUTING.md"),
        RepoPath("docs", "SELF_HOSTING.md"),
        RepoPath("docs", "BACKUP_RESTORE_UPGRADE.md"),
        RepoPath("docs", "RELEASE_CHECKLIST.md"),
    ];

    private static readonly string[] RequiredMetadataKeys =
    [
        "Audience",
        "Status",
        "Owner",
        "Last Verified",
        "Source Anchors",
    ];

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "Implemented",
        "Draft",
        "Planned",
        "Mixed",
    };

    private static readonly HashSet<string> AllowedOwners = new(StringComparer.Ordinal)
    {
        "Platform/Ops",
        "Security",
        "API",
        "Frontend",
        "Product/Admin",
        "Contributor Experience",
        "Agent Context",
    };

    [Test]
    public async Task CanonicalDocs_HaveRequiredMetadata()
    {
        var errors = new List<string>();

        foreach (var file in RequiredMetadataDocs)
        {
            if (!File.Exists(file))
            {
                errors.Add($"Missing canonical documentation file: {Path.GetRelativePath(RepoRoot, file)}");
                continue;
            }

            var metadata = ExtractBlockquoteMetadata(File.ReadAllText(file));
            foreach (var key in RequiredMetadataKeys)
            {
                if (!metadata.ContainsKey(key))
                {
                    errors.Add($"{Path.GetRelativePath(RepoRoot, file)} is missing metadata key '{key}'.");
                }
            }

            if (metadata.TryGetValue("Status", out var status) && !AllowedStatuses.Contains(status))
            {
                errors.Add($"{Path.GetRelativePath(RepoRoot, file)} has unsupported Status '{status}'.");
            }

            if (metadata.TryGetValue("Owner", out var owner) && !AllowedOwners.Contains(owner))
            {
                errors.Add($"{Path.GetRelativePath(RepoRoot, file)} has unsupported Owner '{owner}'.");
            }

            if (metadata.TryGetValue("Last Verified", out var verified) && !Regex.IsMatch(verified, "^\\d{4}-\\d{2}-\\d{2}$"))
            {
                errors.Add($"{Path.GetRelativePath(RepoRoot, file)} Last Verified must use YYYY-MM-DD, found '{verified}'.");
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task CanonicalDocs_SourceAnchorsResolve()
    {
        var errors = new List<string>();

        foreach (var file in RequiredMetadataDocs.Where(File.Exists))
        {
            var metadata = ExtractBlockquoteMetadata(File.ReadAllText(file));
            if (!metadata.TryGetValue("Source Anchors", out var sourceAnchors))
            {
                continue;
            }

            var anchors = Regex.Matches(sourceAnchors, "`([^`]+)`")
                .Select(match => match.Groups[1].Value)
                .ToArray();

            if (anchors.Length == 0)
            {
                errors.Add($"{Path.GetRelativePath(RepoRoot, file)} Source Anchors must include at least one backticked path.");
                continue;
            }

            foreach (var anchor in anchors)
            {
                var resolved = RepoPath(anchor.Split('/', StringSplitOptions.RemoveEmptyEntries));
                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    errors.Add($"{Path.GetRelativePath(RepoRoot, file)} Source Anchor '{anchor}' does not exist.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task CanonicalDocs_DoNotContainPlaceholderMarkers()
    {
        var markerPattern = new Regex("\\{DATE\\}|\\{CONTACT_EMAIL\\}|\\{CONTACT_URL\\}|\\bTBD\\b|coming soon", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var errors = RequiredMetadataDocs
            .Where(File.Exists)
            .SelectMany(file => FindMatches(file, markerPattern, "placeholder or stale status marker"))
            .ToList();

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Docs_DoNotUseUnsupportedTUnitFilterCommands()
    {
        var dotnetFilterPattern = new Regex("dotnet test[^\\n`]*--filter", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var roots = Directory.EnumerateFiles(RepoPath("docs"), "*.md", SearchOption.TopDirectoryOnly)
            .Concat(new[]
            {
                RepoPath(".claude", "commands", "docs-lint.md"),
                RepoPath(".claude", "commands", "check.md"),
                RepoPath(".claude", "commands", "review-pr.md"),
                RepoPath(".github", "PULL_REQUEST_TEMPLATE.md"),
                RepoPath("dev", "HANDOFF_TEMPLATE.md"),
            }.Where(File.Exists));

        var errors = roots
            .SelectMany(file => FindMatches(file, dotnetFilterPattern, "unsupported TUnit filter command"))
            .ToList();

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    private static Dictionary<string, string> ExtractBlockquoteMetadata(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var inMetadataBlock = false;

        foreach (var line in content.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                inMetadataBlock = true;
            }
            else if (inMetadataBlock)
            {
                break;
            }

            var match = Regex.Match(line, "^> \\*\\*(?<key>[^*]+):\\*\\* (?<value>.+)$");
            if (match.Success)
            {
                result[match.Groups["key"].Value] = match.Groups["value"].Value.Trim();
            }
        }

        return result;
    }

    private static IEnumerable<string> FindMatches(string file, Regex pattern, string label)
    {
        var lines = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (pattern.IsMatch(lines[i]))
            {
                yield return $"{Path.GetRelativePath(RepoRoot, file)}:{i + 1}: {label} '{lines[i].Trim()}'";
            }
        }
    }
}
