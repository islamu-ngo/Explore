// ABOUTME: Architecture checks for the IP clean-room skill, intent, rule, and local context links.
// ABOUTME: Keeps the new audit workflow schema-compliant and discoverable without adding parser dependencies.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;

public class AgentContextSchemaTests
{
    [Test]
    public async Task IpCleanRoomSkill_ShouldMatchSchema()
    {
        var root = AgentContextTestFiles.RepositoryRoot;
        var skillPath = Path.Combine(root, ".agents", "skills", "ip-clean-room", "SKILL.md");
        var content = await File.ReadAllTextAsync(skillPath);
        var failures = new List<string>();

        foreach (var required in new[]
                 {
                     "name: ip-clean-room",
                     "description:",
                     "type: guardrail",
                     "enforcement: block",
                     "priority: critical"
                 })
        {
            if (!content.Contains(required, StringComparison.Ordinal))
            {
                failures.Add($"SKILL.md is missing frontmatter value '{required}'.");
            }
        }

        string[] sections =
        [
            "## Purpose",
            "## When to Load",
            "## When NOT to Load",
            "## Must-Read Docs",
            "## Top 5 Invariants",
            "## Top 5 Anti-Patterns",
            "## Minimal Examples",
            "## Verification Hooks",
            "## Related Skills"
        ];

        var previousIndex = -1;
        foreach (var section in sections)
        {
            var index = content.IndexOf(section, StringComparison.Ordinal);
            if (index <= previousIndex)
            {
                failures.Add($"SKILL.md section '{section}' is missing or out of order.");
            }

            previousIndex = index;
        }

        AgentContextTestFiles.RequireFiveNumberedItems(content, "## Top 5 Invariants", "## Top 5 Anti-Patterns", failures);
        AgentContextTestFiles.RequireFiveNumberedItems(content, "## Top 5 Anti-Patterns", "## Minimal Examples", failures);

        if (content.Split('\n').Length > 250)
        {
            failures.Add("SKILL.md exceeds the 250-line schema limit.");
        }

        var resources = Path.Combine(root, ".agents", "skills", "ip-clean-room", "resources");
        foreach (var resource in Directory.GetFiles(resources, "*.md").Order(StringComparer.Ordinal))
        {
            var lines = await File.ReadAllLinesAsync(resource);
            if (lines.Length < 2 || !lines[0].Contains("ABOUTME:", StringComparison.Ordinal) || !lines[1].Contains("ABOUTME:", StringComparison.Ordinal))
            {
                failures.Add($"{Path.GetRelativePath(root, resource)} must start with two ABOUTME lines.");
            }
        }

        await Assert.That(failures).IsEmpty()
            .Because("the ip-clean-room skill must satisfy _SKILL_SCHEMA.md without a skip exception");
    }
}

public class AgentContextIntentManifestTests
{
    [Test]
    public async Task IpCleanRoomGovernance_ShouldBeRegisteredAcrossContextLayers()
    {
        var root = AgentContextTestFiles.RepositoryRoot;
        var manifest = await File.ReadAllTextAsync(Path.Combine(root, ".claude", "contract", "intents.yaml"));
        var rule = await File.ReadAllTextAsync(Path.Combine(root, ".claude", "rules", "ip-clean-room.md"));
        var review = await File.ReadAllTextAsync(Path.Combine(root, ".claude", "commands", "review-pr.md"));
        var template = await File.ReadAllTextAsync(Path.Combine(root, ".github", "PULL_REQUEST_TEMPLATE.md"));
        var failures = new List<string>();

        foreach (var required in new[]
                 {
                     "id: ip-clean-room-governance",
                     "- ip-clean-room",
                     "- .claude/rules/ip-clean-room.md",
                     "- docs/legal/IP_GOVERNANCE.md"
                 })
        {
            if (!manifest.Contains(required, StringComparison.Ordinal))
            {
                failures.Add($"intents.yaml is missing '{required}'.");
            }
        }

        foreach (var requiredPath in new[] { "src/**/*", "docs/**/*", "dev/active/**/*" })
        {
            if (!rule.Contains(requiredPath, StringComparison.Ordinal))
            {
                failures.Add($"ip-clean-room.md is missing path '{requiredPath}'.");
            }
        }

        if (!review.Contains("IP provenance and dependency compatibility", StringComparison.Ordinal)
            || !template.Contains("IP / Clean-Room / Dependency Provenance", StringComparison.Ordinal))
        {
            failures.Add("PR review surfaces must require IP provenance and dependency compatibility evidence.");
        }

        await Assert.That(failures).IsEmpty()
            .Because("the clean-room guardrail must be routed through intents, path rules, and PR review");
    }
}

public class AgentContextLinkTests
{
    private static readonly Regex MarkdownLink = new(@"\[[^\]]+\]\((?<target>[^)]+)\)", RegexOptions.Compiled);

    [Test]
    public async Task IpCleanRoomContextFiles_ShouldHaveResolvableLocalLinks()
    {
        var root = AgentContextTestFiles.RepositoryRoot;
        string[] files =
        [
            "AGENTS.md",
            "docs/QUICK_REFERENCE.md",
            "docs/index.md",
            "docs/legal/IP_GOVERNANCE.md",
            ".agents/skills/ip-clean-room/SKILL.md",
            ".claude/rules/ip-clean-room.md"
        ];
        var paths = files.Select(path => Path.Combine(root, path))
            .Concat(Directory.GetFiles(Path.Combine(root, ".agents", "skills", "ip-clean-room", "resources"), "*.md"));
        var failures = new List<string>();

        foreach (var path in paths)
        {
            var content = await File.ReadAllTextAsync(path);
            foreach (Match match in MarkdownLink.Matches(content))
            {
                var target = match.Groups["target"].Value;
                if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    || target.StartsWith('#'))
                {
                    continue;
                }

                var localPath = Uri.UnescapeDataString(target.Split('#', 2)[0]);
                var resolved = Path.GetFullPath(localPath, Path.GetDirectoryName(path)!);
                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    failures.Add($"{Path.GetRelativePath(root, path)} -> {target}");
                }
            }
        }

        await Assert.That(failures).IsEmpty()
            .Because("every local clean-room context link must resolve from a cold start");
    }
}

internal static class AgentContextTestFiles
{
    internal static string RepositoryRoot { get; } = FindRepositoryRoot();

    internal static void RequireFiveNumberedItems(string content, string startHeading, string endHeading, ICollection<string> failures)
    {
        var start = content.IndexOf(startHeading, StringComparison.Ordinal);
        var end = content.IndexOf(endHeading, start + startHeading.Length, StringComparison.Ordinal);
        if (start < 0 || end < 0)
        {
            failures.Add($"Cannot inspect numbered items between '{startHeading}' and '{endHeading}'.");
            return;
        }

        var count = Regex.Matches(content[start..end], @"^\d+\.", RegexOptions.Multiline).Count;
        if (count != 5)
        {
            failures.Add($"'{startHeading}' must contain exactly five numbered items; found {count}.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Explore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Explore.slnx.");
    }
}
