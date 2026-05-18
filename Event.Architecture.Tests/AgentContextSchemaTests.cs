// ABOUTME: Validates structure of .claude/rules/, .claude/skills/, and .claude/agents/ files.
// ABOUTME: Enforces the schemas defined in _schema.md, _SKILL_SCHEMA.md, and _AGENT_SCHEMA.md.

namespace Event.Architecture.Tests;

using static ContextSystemHelpers;

public class AgentContextSchemaTests
{
    private static readonly string[] RequiredRuleFrontmatterKeys =
    {
        "name", "description", "paths", "related_skills",
        "related_docs", "minimum_tests", "related_intents",
    };

    private static readonly string[] RequiredSkillFrontmatterKeys =
    {
        "name", "description", "type", "enforcement", "priority",
    };

    private static readonly string[] RequiredSkillSections =
    {
        "Purpose", "When to Load", "When NOT to Load", "Must-Read Docs",
        "Top 5 Invariants", "Top 5 Anti-Patterns", "Minimal Examples",
        "Verification Hooks", "Related Skills",
    };

    private static readonly HashSet<string> SkipSchemaMigration = new(StringComparer.OrdinalIgnoreCase)
    {
        "accessibility", "agentic-research", "aspire",
        "blazor-bff-patterns", "blazor-css-isolation", "conventional-commit",
        "design-system", "error-tracking", "footer-management", "gitkraken-cli",
        "outbox-pattern", "prd",
    };

    private static readonly string[] RequiredAgentFrontmatterKeys =
    {
        "name", "description", "type", "enforcement", "priority", "tools",
    };

    private static readonly string[] RequiredAgentSections =
    {
        "Purpose", "When to Use", "When NOT to Use", "Mandatory Reads",
        "Allowed Tools", "Forbidden Moves", "Output Contract",
        "Done Criteria", "Anti-Patterns", "Related Agents",
    };

    private const int SkillMaxLines = 250;
    private const int AgentMaxLines = 160;
    private const int ClaudeBootloaderMaxLines = 150;

    [Test]
    public async Task ClaudeBootloader_UnderMaxLineCount()
    {
        var path = RepoPath("AGENTS.md");
        var lines = CountLines(path);
        await Assert.That(lines).IsLessThanOrEqualTo(ClaudeBootloaderMaxLines)
            .Because($"AGENTS.md has {lines} lines, exceeds bootloader max of {ClaudeBootloaderMaxLines}. Move operational prose to docs/OPERATIONS.md.");
    }

    [Test]
    public async Task Rules_AllPathScopedFilesHaveRequiredFrontmatter()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateRuleFiles())
        {
            var content = File.ReadAllText(file);
            var (frontmatter, _) = ParseMarkdown(content);
            foreach (var key in RequiredRuleFrontmatterKeys)
            {
                if (!frontmatter.ContainsKey(key))
                {
                    errors.Add($"{Path.GetFileName(file)} is missing required frontmatter key '{key}'.");
                }
            }
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Skills_MigratedSkillsHaveRequiredFrontmatter()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateMigratedSkillFiles())
        {
            var content = File.ReadAllText(file);
            var (frontmatter, _) = ParseMarkdown(content);
            foreach (var key in RequiredSkillFrontmatterKeys)
            {
                if (!frontmatter.ContainsKey(key))
                {
                    errors.Add($"{SkillName(file)} is missing frontmatter key '{key}'.");
                }
            }
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Skills_MigratedSkillsHaveRequiredSectionsInOrder()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateMigratedSkillFiles())
        {
            var content = File.ReadAllText(file);
            var (_, body) = ParseMarkdown(content);
            var sections = ExtractH2Sections(body);
            AssertSectionsInOrder(file, sections, RequiredSkillSections, errors);
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Skills_MigratedSkillsUnderMaxLineCount()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateMigratedSkillFiles())
        {
            var lines = CountLines(file);
            if (lines > SkillMaxLines)
            {
                errors.Add($"{SkillName(file)} has {lines} lines, exceeds max {SkillMaxLines}.");
            }
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Agents_AllAgentsHaveRequiredFrontmatter()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateAgentFiles())
        {
            var content = File.ReadAllText(file);
            var (frontmatter, _) = ParseMarkdown(content);
            foreach (var key in RequiredAgentFrontmatterKeys)
            {
                if (!frontmatter.ContainsKey(key))
                {
                    errors.Add($"{Path.GetFileName(file)} is missing frontmatter key '{key}'.");
                }
            }
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Agents_AllAgentsHaveRequiredSectionsInOrder()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateAgentFiles())
        {
            var content = File.ReadAllText(file);
            var (_, body) = ParseMarkdown(content);
            var sections = ExtractH2Sections(body);
            AssertSectionsInOrder(file, sections, RequiredAgentSections, errors);
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Agents_MandatoryReadsIncludesCanonicalArtifacts()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateAgentFiles())
        {
            var content = File.ReadAllText(file);
            var mandatoryReadsBlock = ExtractSection(content, "Mandatory Reads");
            if (!mandatoryReadsBlock.Contains("AGENTS.md", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{Path.GetFileName(file)} 'Mandatory Reads' must reference AGENTS.md.");
            }
            if (!mandatoryReadsBlock.Contains("QUICK_REFERENCE.md", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{Path.GetFileName(file)} 'Mandatory Reads' must reference docs/QUICK_REFERENCE.md.");
            }
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task Agents_UnderMaxLineCount()
    {
        var errors = new List<string>();
        foreach (var file in EnumerateAgentFiles())
        {
            var lines = CountLines(file);
            if (lines > AgentMaxLines)
            {
                errors.Add($"{Path.GetFileName(file)} has {lines} lines, exceeds max {AgentMaxLines}.");
            }
        }
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    private static IEnumerable<string> EnumerateRuleFiles()
    {
        var dir = RepoPath(".claude", "rules");
        if (!Directory.Exists(dir)) { yield break; }
        foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
        {
            var name = Path.GetFileName(file);
            if (name is "README.md" or "_schema.md") { continue; }
            yield return file;
        }
    }

    private static IEnumerable<string> EnumerateMigratedSkillFiles()
    {
        var dir = RepoPath(".claude", "skills");
        if (!Directory.Exists(dir)) { yield break; }
        foreach (var sub in Directory.EnumerateDirectories(dir))
        {
            var name = Path.GetFileName(sub);
            if (SkipSchemaMigration.Contains(name)) { continue; }
            var skill = Path.Combine(sub, "SKILL.md");
            if (File.Exists(skill)) { yield return skill; }
        }
    }

    private static IEnumerable<string> EnumerateAgentFiles()
    {
        var dir = RepoPath(".claude", "agents");
        if (!Directory.Exists(dir)) { yield break; }
        foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
        {
            var name = Path.GetFileName(file);
            if (name is "README.md" or "_AGENT_SCHEMA.md") { continue; }
            yield return file;
        }
    }

    private static string SkillName(string path) =>
        Path.GetFileName(Path.GetDirectoryName(path)!);

    private static void AssertSectionsInOrder(string file, List<string> actualSections, string[] required, List<string> errors)
    {
        int searchFrom = 0;
        foreach (var expected in required)
        {
            int idx = actualSections.FindIndex(searchFrom, s => s.Equals(expected, StringComparison.Ordinal));
            if (idx < 0)
            {
                errors.Add($"{Path.GetFileName(file)} is missing required section '## {expected}' in expected order. Found sections: {string.Join(", ", actualSections)}");
                return;
            }
            searchFrom = idx + 1;
        }
    }

    private static string ExtractSection(string content, string sectionTitle)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var header = $"## {sectionTitle}";
        var sb = new System.Text.StringBuilder();
        bool inside = false;
        foreach (var line in lines)
        {
            if (line.TrimEnd().Equals(header, StringComparison.Ordinal))
            {
                inside = true;
                continue;
            }
            if (inside)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal)) { break; }
                sb.AppendLine(line);
            }
        }
        return sb.ToString();
    }
}
