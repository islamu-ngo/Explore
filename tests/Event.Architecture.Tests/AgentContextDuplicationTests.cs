// ABOUTME: Blocks reintroduction of duplicated project-context blocks across .claude/agents/*.md.
// ABOUTME: Detection uses consecutive-line match + Jaccard similarity on line hashes, plus a blacklist of stack-overview phrases.

namespace Event.Architecture.Tests;

using System.Security.Cryptography;
using System.Text;
using static ContextSystemHelpers;

public class AgentContextDuplicationTests
{
    private const int MinConsecutiveDuplicateLines = 15;
    private const double MaxJaccardSimilarity = 0.85;

    // Phrases that indicate stack-overview or project-context content has leaked into an agent file.
    private static readonly string[] ForbiddenPhrases =
    [
        "This repo uses .NET",
        "This project uses .NET",
        ".NET 10 +",
        ".NET 10 + Blazor",
        "Clean Architecture, CQRS",
        "Backend for Frontend (BFF)",
        "Blazor Server + WebAssembly",
        "PostgreSQL + PostGIS",
        "Keycloak (OIDC/JWT)",
    ];

    [Test]
    public async Task NoAgentContainsStackOverviewPhrases()
    {
        var files = EnumerateAgentFiles();
        var errors = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var phrase in ForbiddenPhrases)
            {
                if (content.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = Path.GetRelativePath(RepoRoot, file);
                    errors.Add($"{relative}: contains forbidden stack-overview phrase '{phrase}'. Link to AGENTS.md instead.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task NoPairOfAgentsHasLongConsecutiveDuplicateBlock()
    {
        await AssertNoLongDuplicateRuns(EnumerateAgentFiles(), MinConsecutiveDuplicateLines, "Agents");
    }

    [Test]
    public async Task NoPairOfRulesHasLongConsecutiveDuplicateBlock()
    {
        await AssertNoLongDuplicateRuns(EnumerateRuleFiles(), 10, "Rules");
    }

    [Test]
    public async Task NoRuleDuplicatesQuickReference()
    {
        var quickRef = RepoPath("docs", "QUICK_REFERENCE.md");
        var quickRefLines = MeaningfulLines(quickRef);
        var errors = new List<string>();

        foreach (var ruleFile in EnumerateRuleFiles())
        {
            var ruleLines = MeaningfulLines(ruleFile);
            var run = LongestCommonLineRun(quickRefLines, ruleLines);
            if (run >= 8) // Lower threshold for QuickRef overlap
            {
                var relative = Path.GetRelativePath(RepoRoot, ruleFile);
                errors.Add($"{relative}: duplicates {run} lines from QUICK_REFERENCE.md. Link to anchors instead of restating invariants.");
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task NoPairOfAgentsExceedsJaccardSimilarity()
    {
        var files = EnumerateAgentFiles().ToList();
        var errors = new List<string>();

        var hashes = files.ToDictionary(f => f, f => MeaningfulLines(f).Select(HashLine).ToHashSet());

        for (int i = 0; i < files.Count; i++)
        {
            for (int j = i + 1; j < files.Count; j++)
            {
                var a = hashes[files[i]];
                var b = hashes[files[j]];
                if (a.Count == 0 || b.Count == 0)
                {
                    continue;
                }
                var intersection = a.Intersect(b).Count();
                var union = a.Union(b).Count();
                var jaccard = union == 0 ? 0.0 : (double)intersection / union;
                if (jaccard >= MaxJaccardSimilarity)
                {
                    var pa = Path.GetRelativePath(RepoRoot, files[i]);
                    var pb = Path.GetRelativePath(RepoRoot, files[j]);
                    errors.Add($"{pa} and {pb}: Jaccard similarity {jaccard:F2} >= {MaxJaccardSimilarity}. Deduplicate by pointing to a shared canonical source.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    private async Task AssertNoLongDuplicateRuns(IEnumerable<string> filesList, int threshold, string category)
    {
        var files = filesList.ToList();
        var errors = new List<string>();
        var signatures = files.ToDictionary(f => f, f => MeaningfulLines(f));

        for (int i = 0; i < files.Count; i++)
        {
            for (int j = i + 1; j < files.Count; j++)
            {
                var run = LongestCommonLineRun(signatures[files[i]], signatures[files[j]]);
                if (run >= threshold)
                {
                    var a = Path.GetRelativePath(RepoRoot, files[i]);
                    var b = Path.GetRelativePath(RepoRoot, files[j]);
                    errors.Add($"{a} and {b}: share {run} consecutive identical meaningful lines (limit: {threshold - 1}).");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because($"{category} duplication detected:\n{string.Join("\n", errors)}");
    }

    private static IEnumerable<string> EnumerateRuleFiles()
    {
        var dir = RepoPath(".claude", "rules");
        if (!Directory.Exists(dir)) { return Array.Empty<string>(); }
        return Directory.EnumerateFiles(dir, "*.md")
            .Where(f => !new[] { "README.md", "_schema.md" }.Contains(Path.GetFileName(f)))
            .OrderBy(f => f);
    }

    private static IEnumerable<string> EnumerateAgentFiles()
    {
        var dir = RepoPath(".claude", "agents");
        if (!Directory.Exists(dir))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(dir, "*.md", SearchOption.TopDirectoryOnly)
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return !string.Equals(name, "README.md", StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(name, "_AGENT_SCHEMA.md", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(f => f, StringComparer.Ordinal);
    }

    // Returns non-trivial lines: drops frontmatter delimiters, ABOUTME, section headers, empty/short lines.
    // Reduces false positives from schema boilerplate that every agent legitimately shares.
    private static List<string> MeaningfulLines(string path)
    {
        var all = File.ReadAllLines(path).Select(l => l.Trim()).ToList();
        var result = new List<string>(all.Count);
        bool inFrontmatter = false;
        foreach (var line in all)
        {
            if (line == "---")
            {
                inFrontmatter = !inFrontmatter;
                continue;
            }
            if (inFrontmatter)
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            if (line.StartsWith("<!--") || line.StartsWith("#"))
            {
                continue;
            }
            // Drop very short lines (bullets, table dividers) that create spurious matches.
            if (line.Length < 10)
            {
                continue;
            }
            result.Add(line);
        }
        return result;
    }

    private static string HashLine(string line)
    {
        var normalized = line.Normalize(NormalizationForm.FormC);
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    private static int LongestCommonLineRun(List<string> a, List<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        // Standard LCS-of-runs DP in O(|a|*|b|); sizes are <= ~100 each so this is cheap.
        var dp = new int[a.Count + 1, b.Count + 1];
        int best = 0;
        for (int i = 1; i <= a.Count; i++)
        {
            for (int j = 1; j <= b.Count; j++)
            {
                if (a[i - 1] == b[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                    if (dp[i, j] > best)
                    {
                        best = dp[i, j];
                    }
                }
            }
        }
        return best;
    }
}
