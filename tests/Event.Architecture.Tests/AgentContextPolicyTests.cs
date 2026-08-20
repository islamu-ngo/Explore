// ABOUTME: Enforces canonical context routes, agent model tiers, and bounded context-budget ownership.
// ABOUTME: Prevents stale agent paths and generic dev context files from reintroducing duplicate reads.

namespace Event.Architecture.Tests;

public class AgentContextPolicyTests
{
    private static readonly string[] AllowedModelTiers = ["economical", "balanced", "advanced"];
    private static readonly string[] AllowedSkillTypes = ["guardrail", "pattern", "reference", "workflow"];
    private static readonly string[] AllowedSkillEnforcement = ["block", "suggest", "inform"];
    private static readonly string[] AllowedSkillPriorities = ["critical", "high", "medium", "low"];

    [Test]
    [DisplayName("Repository agent profiles must declare an allowed model tier")]
    public async Task AgentProfiles_ShouldDeclareAllowedModelTier()
    {
        var root = FindRepoRoot();
        var agentDirectory = Path.Combine(root, ".agents", "agents");
        var violations = Directory.GetFiles(agentDirectory, "*-agent.md")
            .Select(path => new
            {
                Path = Path.GetRelativePath(root, path),
                Tier = File.ReadLines(path)
                    .FirstOrDefault(line => line.StartsWith("model_tier:", StringComparison.Ordinal))?
                    .Split(':', 2)[1]
                    .Trim()
            })
            .Where(profile => profile.Tier is null || !AllowedModelTiers.Contains(profile.Tier, StringComparer.Ordinal))
            .Select(profile => $"{profile.Path}: invalid or missing model_tier")
            .ToArray();

        await Assert.That(violations).IsEmpty()
            .Because("every repository agent must select an economical, balanced, or advanced capability tier");
    }

    [Test]
    [DisplayName("Agent-facing context routes must use canonical .agents paths")]
    public async Task AgentFacingFiles_ShouldNotReferenceRetiredContextRoutes()
    {
        var root = FindRepoRoot();
        string[] retiredRoutes =
        [
            ".claude/contract",
            ".claude/rules",
            ".claude/commands",
            ".claude/hooks",
            ".Codex/contract",
            "dev/active/README.md",
            "dev/HANDOFF_TEMPLATE.md",
            "dotnet test --project Event.Architecture.Tests/",
            "AgentContextSchemaTests",
            "AgentContextLinkTests"
        ];

        var files = Directory.GetFiles(Path.Combine(root, ".agents"), "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".md", StringComparison.Ordinal) || path.EndsWith(".yaml", StringComparison.Ordinal))
            .Concat(
            [
                Path.Combine(root, "AGENTS.md"),
                Path.Combine(root, "README.md"),
                Path.Combine(root, ".github", "copilot-instructions.md")
            ]);

        var violations = files
            .SelectMany(path => retiredRoutes
                .Where(route => File.ReadAllText(path).Contains(route, StringComparison.Ordinal))
                .Select(route => $"{Path.GetRelativePath(root, path)}: {route}"))
            .ToArray();

        await Assert.That(violations).IsEmpty()
            .Because("agent-facing guidance must use canonical .agents routes without deleted generic dev context files");
    }

    [Test]
    [DisplayName("Context budgets must have one machine-readable source")]
    public async Task ContextBudget_ShouldExposeRequiredMachineReadableKeys()
    {
        var root = FindRepoRoot();
        var budgetManifest = File.ReadAllText(Path.Combine(root, ".agents", "benchmarks", "cold-start-tasks.yaml"));
        string[] requiredKeys =
        [
            "additional_bootstrap_bytes:",
            "discovery_result_bytes:",
            "single_retrieval_bytes:",
            "scout_result_characters:",
            "duplicate_unchanged_bytes:",
            "full_registry_reads:"
        ];

        var missingKeys = requiredKeys
            .Where(key => !budgetManifest.Contains(key, StringComparison.Ordinal))
            .ToArray();

        await Assert.That(missingKeys).IsEmpty()
            .Because("cold-start benchmarks are the single machine-readable source for context limits");
        await Assert.That(File.Exists(Path.Combine(root, "dev", "active", "README.md"))).IsFalse();
        await Assert.That(File.Exists(Path.Combine(root, "dev", "HANDOFF_TEMPLATE.md"))).IsFalse();
    }

    [Test]
    [DisplayName("Skill routers must remain selectable before loading and compact after loading")]
    public async Task SkillRouters_ShouldHaveRoutingMetadataWithoutActivationDuplication()
    {
        var root = FindRepoRoot();
        var violations = new List<string>();

        foreach (var path in Directory.GetFiles(Path.Combine(root, ".agents", "skills"), "SKILL.md", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(root, path);
            var lines = File.ReadAllLines(path);

            if (lines.FirstOrDefault() != "---")
            {
                violations.Add($"{relativePath}: YAML frontmatter must be first");
            }

            var metadata = lines
                .Skip(1)
                .TakeWhile(line => line != "---")
                .Where(line => line.Contains(':'))
                .Select(line => line.Split(':', 2))
                .ToDictionary(parts => parts[0], parts => parts[1].Trim().Trim('"'));

            foreach (var key in new[] { "name", "description", "type", "enforcement", "priority" })
            {
                if (!metadata.ContainsKey(key))
                {
                    violations.Add($"{relativePath}: missing {key}");
                }
            }

            if (metadata.TryGetValue("name", out var name)
                && name != Path.GetFileName(Path.GetDirectoryName(path)))
            {
                violations.Add($"{relativePath}: name does not match folder");
            }

            if (metadata.TryGetValue("description", out var description)
                && (!description.StartsWith("Load ", StringComparison.Ordinal) || description.Length > 400))
            {
                violations.Add($"{relativePath}: description must be a compact pre-load routing boundary");
            }

            if (metadata.TryGetValue("type", out var type) && !AllowedSkillTypes.Contains(type))
            {
                violations.Add($"{relativePath}: invalid type {type}");
            }

            if (metadata.TryGetValue("enforcement", out var enforcement) && !AllowedSkillEnforcement.Contains(enforcement))
            {
                violations.Add($"{relativePath}: invalid enforcement {enforcement}");
            }

            if (metadata.TryGetValue("priority", out var priority) && !AllowedSkillPriorities.Contains(priority))
            {
                violations.Add($"{relativePath}: invalid priority {priority}");
            }

            if (new FileInfo(path).Length > 6 * 1024)
            {
                violations.Add($"{relativePath}: router exceeds 6 KB");
            }

            if (lines.Count(line => line.Contains("ABOUTME:", StringComparison.Ordinal)) < 2)
            {
                violations.Add($"{relativePath}: requires two ABOUTME lines");
            }

            string[] duplicatedRoutingHeadings =
            [
                "## When to Load",
                "## When NOT to Load",
                "## When This Skill Activates",
                "## When to Use",
                "## Activation",
                "## Keywords",
                "## File Patterns"
            ];

            foreach (var heading in duplicatedRoutingHeadings.Where(heading => lines.Contains(heading)))
            {
                violations.Add($"{relativePath}: duplicates catalog routing in {heading}");
            }
        }

        await Assert.That(violations).IsEmpty()
            .Because("agents select skills from name and description before SKILL.md enters context");
    }

    private static string FindRepoRoot()
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
