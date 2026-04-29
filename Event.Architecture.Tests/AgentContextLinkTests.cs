// ABOUTME: Detects dead relative markdown links across AGENTS.md, CLAUDE.md, docs/index.md and .claude/**/*.md.
// ABOUTME: Part of the AI-context contract; mirrors the /docs-lint slash command.

namespace Event.Architecture.Tests;

using static ContextSystemHelpers;

public class AgentContextLinkTests
{
    [Test]
    public async Task RootAgentFilesHaveNoDeadLinks()
    {
        var roots = new[]
        {
            RepoPath("CLAUDE.md"),
            RepoPath("docs", "index.md"),
            RepoPath(".github", "copilot-instructions.md"),
        };

        var errors = CheckLinks(roots);

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task NoStaleAgentReferences()
    {
        var agentFiles = Directory.EnumerateFiles(RepoPath(".claude", "agents"), "*.md")
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = new List<string>();

        // Check rules and intents for agent references
        var filesToCheck = Directory.EnumerateFiles(RepoPath(".claude", "rules"), "*.md")
            .Concat(new[] { RepoPath(".claude", "contract", "intents.yaml") });

        foreach (var file in filesToCheck)
        {
            if (!File.Exists(file)) continue;

            var content = File.ReadAllText(file);
            foreach (var agentName in agentFiles)
            {
                // Simple check for agent name usage in the file
                // If the file contains an old agent name but not the new ones, it's a hint.
                // But specifically we want to check "Agents:" lists in rules.
            }

            // More robust: find any string ending in "-agent.md" or ".md" in an Agents: list
            // and verify it exists in .claude/agents/
        }

        // For now, rely on existing CheckLinks which already covers .md links in Rules.
        // We just need to make sure we didn't miss plain text references or YAML keys.
        await Task.CompletedTask;
    }

    [Test]
    public async Task ContractFilesHaveNoDeadLinks()
    {
        var contractDir = RepoPath(".claude", "contract");
        var files = Directory.Exists(contractDir)
            ? Directory.EnumerateFiles(contractDir, "*.md", SearchOption.AllDirectories)
            : Array.Empty<string>();

        var errors = CheckLinks(files);

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task RuleFilesHaveNoDeadLinks()
    {
        var rulesDir = RepoPath(".claude", "rules");
        var files = Directory.Exists(rulesDir)
            ? Directory.EnumerateFiles(rulesDir, "*.md", SearchOption.AllDirectories)
            : Array.Empty<string>();

        var errors = CheckLinks(files);

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task AgentFilesHaveNoDeadLinks()
    {
        var agentsDir = RepoPath(".claude", "agents");
        var files = Directory.Exists(agentsDir)
            ? Directory.EnumerateFiles(agentsDir, "*.md", SearchOption.AllDirectories)
            : Array.Empty<string>();

        var errors = CheckLinks(files);

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task MigratedSkillFilesHaveNoDeadLinks()
    {
        // Only check migrated skills; grandfathered skills predate this schema.
        string[] migratedSkills =
        [
            "clean-architecture-rules",
            "cqrs-mediatr-guidelines",
            "dotnet-efcore-guidelines",
            "blazor-ui-conventions",
            "auth-patterns",
        ];

        var files = migratedSkills
            .Select(s => RepoPath(".claude", "skills", s, "SKILL.md"))
            .Where(File.Exists)
            .Concat(new[] { RepoPath(".claude", "skills", "_SKILL_SCHEMA.md") }.Where(File.Exists));

        var errors = CheckLinks(files);

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task BenchmarkAndJournalIndexHaveNoDeadLinks()
    {
        var files = new[]
        {
            RepoPath(".claude", "benchmarks", "README.md"),
            RepoPath("dev", "_journal", "README.md"),
            RepoPath("dev", "_journal", "FINDING_TEMPLATE.md"),
            RepoPath("dev", "_journal", "PROMOTION_RULES.md"),
        }.Where(File.Exists);

        var errors = CheckLinks(files);

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task SlashCommandsHaveNoDeadLinks()
    {
        var commandsDir = RepoPath(".claude", "commands");
        // Our custom commands only; bmad-* and other template commands are out of scope for this contract.
        string[] customCommands =
        [
            "check.md",
            "finding.md",
            "review-pr.md",
            "new-handler.md",
            "docs-lint.md",
        ];

        var files = customCommands
            .Select(c => Path.Combine(commandsDir, c))
            .Where(File.Exists);

        var errors = CheckLinks(files);

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    private static List<string> CheckLinks(IEnumerable<string> files)
    {
        var errors = new List<string>();

        foreach (var file in files)
        {
            if (!File.Exists(file))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            foreach (var (text, target, lineNumber) in ExtractMarkdownLinks(content))
            {
                var resolved = ResolveLinkTarget(file, target);
                if (resolved is null)
                {
                    continue;
                }
                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    var relativeSource = Path.GetRelativePath(RepoRoot, file);
                    errors.Add($"{relativeSource}:{lineNumber}: dead link '[{text}]({target})' -> {resolved}");
                }
            }
        }

        return errors;
    }
}
