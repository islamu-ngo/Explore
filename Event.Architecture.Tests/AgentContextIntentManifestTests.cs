// ABOUTME: Validates .claude/contract/intents.yaml structure, field completeness, and cross-references.
// ABOUTME: Every referenced doc, skill, rule, agent, and test project must exist on disk.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;
using static ContextSystemHelpers;

public class AgentContextIntentManifestTests
{
    private static readonly string[] RequiredKeys =
    [
        "id",
        "title",
        "category",
        "triggers",
        "must_read_docs",
        "load_skills",
        "load_rules",
        "paths_in_scope",
        "minimum_tests",
        "docs_to_update",
        "unique_acceptance",
        "forbidden_without_approval",
    ];

    private static readonly HashSet<string> AllowedCategories =
    [
        "api", "application", "persistence", "domain", "blazor",
        "auth", "authorization", "infrastructure", "federation", "cross-cutting",
    ];

    // Test project names valid as minimum_tests values.
    private static readonly HashSet<string> KnownTestProjects =
    [
        "Event.Application.UnitTests",
        "Event.Domain.UnitTests",
        "Event.Architecture.Tests",
        "Event.Persistence.IntegrationTests",
        "Event.API.IntegrationTests",
        "Explore.Infrastructure.Tests",
        "Explore.Blazor.IntegrationTests",
        "Explore.Blazor.Client.Tests",
        "Explore.Blazor.Client.E2ETests",
        "Explore.Secrets.UnitTests",
    ];

    private static readonly char[] WildcardChars = ['*', '?'];

    [Test]
    public async Task IntentsYamlExists()
    {
        var path = RepoPath(".claude", "contract", "intents.yaml");
        await Assert.That(File.Exists(path)).IsTrue().Because($"Missing {path}");
    }

    [Test]
    public async Task EveryIntentHasAllRequiredKeys()
    {
        var intents = LoadIntents();
        var errors = new List<string>();

        foreach (var intent in intents)
        {
            foreach (var key in RequiredKeys)
            {
                // A key may be a scalar (Fields) OR a list (Lists). Either form satisfies "present".
                if (!intent.Fields.ContainsKey(key) && !intent.Lists.ContainsKey(key))
                {
                    errors.Add($"Intent '{intent.Id}' missing required key '{key}'.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryIntentIdIsKebabCase()
    {
        var intents = LoadIntents();
        var pattern = new Regex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$");
        var errors = intents
            .Where(i => !pattern.IsMatch(i.Id))
            .Select(i => $"Intent id '{i.Id}' is not kebab-case.")
            .ToList();

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryCategoryIsInEnum()
    {
        var intents = LoadIntents();
        var errors = intents
            .Where(i => i.Fields.TryGetValue("category", out var c) && !AllowedCategories.Contains(c))
            .Select(i => $"Intent '{i.Id}' has unknown category '{i.Fields["category"]}'.")
            .ToList();

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryMustReadDocExists()
    {
        var intents = LoadIntents();
        var errors = new List<string>();

        foreach (var intent in intents)
        {
            foreach (var doc in intent.ListField("must_read_docs"))
            {
                var full = RepoPath(doc.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full))
                {
                    errors.Add($"Intent '{intent.Id}': must_read_docs path '{doc}' does not exist.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryLoadedRuleExists()
    {
        var intents = LoadIntents();
        var errors = new List<string>();

        foreach (var intent in intents)
        {
            foreach (var rule in intent.ListField("load_rules"))
            {
                var full = RepoPath(rule.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(full))
                {
                    errors.Add($"Intent '{intent.Id}': load_rules path '{rule}' does not exist.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryLoadedSkillExists()
    {
        var intents = LoadIntents();
        var skillsDir = RepoPath(".agents", "skills");
        var errors = new List<string>();

        foreach (var intent in intents)
        {
            foreach (var skill in intent.ListField("load_skills"))
            {
                var skillFolder = Path.Combine(skillsDir, skill);
                var skillMd = Path.Combine(skillFolder, "SKILL.md");
                if (!Directory.Exists(skillFolder) || !File.Exists(skillMd))
                {
                    errors.Add($"Intent '{intent.Id}': load_skills entry '{skill}' has no matching SKILL.md at {skillMd}.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryMinimumTestIsKnownProject()
    {
        var intents = LoadIntents();
        var errors = new List<string>();

        foreach (var intent in intents)
        {
            foreach (var test in intent.ListField("minimum_tests"))
            {
                if (!KnownTestProjects.Contains(test))
                {
                    errors.Add($"Intent '{intent.Id}': minimum_tests entry '{test}' is not a known test project. Allowed: {string.Join(", ", KnownTestProjects)}.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryRelatedAgentExists()
    {
        var intents = LoadIntents();
        var agentsDir = RepoPath(".claude", "agents");
        var errors = new List<string>();

        foreach (var intent in intents)
        {
            foreach (var agent in intent.ListField("related_agents"))
            {
                var file = Path.Combine(agentsDir, $"{agent}.md");
                if (!File.Exists(file))
                {
                    errors.Add($"Intent '{intent.Id}': related_agents entry '{agent}' has no matching file at {file}.");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    [Test]
    public async Task EveryPathsInScopeResolvesToSomething()
    {
        // Smoke test: first path prefix before any wildcard must exist as a directory or at least map to a valid project prefix.
        var intents = LoadIntents();
        var errors = new List<string>();

        foreach (var intent in intents)
        {
            foreach (var pathGlob in intent.ListField("paths_in_scope"))
            {
                var prefix = pathGlob.Split(WildcardChars, StringSplitOptions.None)[0].TrimEnd('/');
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }
                var full = RepoPath(prefix.Replace('/', Path.DirectorySeparatorChar));
                var dir = Directory.Exists(full);
                var file = File.Exists(full);
                if (!dir && !file)
                {
                    // Fallback: check if the parent directory of the prefix exists (supports filename-prefix wildcards like 'Ai*.cs')
                    var parentDir = Path.GetDirectoryName(full);
                    if (parentDir != null && Directory.Exists(parentDir))
                    {
                        continue;
                    }
                    errors.Add($"Intent '{intent.Id}': paths_in_scope glob '{pathGlob}' does not resolve (prefix '{prefix}' not found).");
                }
            }
        }

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors));
    }

    private record Intent(string Id, Dictionary<string, string> Fields, Dictionary<string, List<string>> Lists)
    {
        public IEnumerable<string> ListField(string key) =>
            Lists.TryGetValue(key, out var items) ? items : Enumerable.Empty<string>();
    }

    // Narrow YAML reader specific to our intents.yaml shape: top-level `intents:` list of mapping blocks;
    // each mapping has scalar fields and simple string-list fields. Avoids adding a YAML package dependency.
    private static List<Intent> LoadIntents()
    {
        var path = RepoPath(".claude", "contract", "intents.yaml");
        var raw = File.ReadAllText(path);
        var lines = raw.Replace("\r\n", "\n").Split('\n');

        var intents = new List<Intent>();
        Intent? current = null;
        string? currentListKey = null;
        int intentIndent = -1;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var trimmedStart = line.TrimStart();
            if (trimmedStart.Length == 0 || trimmedStart.StartsWith('#'))
            {
                continue;
            }

            int indent = line.Length - trimmedStart.Length;

            // New intent entry starts at top-level `intents:` list with `- id: ...`.
            if (trimmedStart.StartsWith("- id:", StringComparison.Ordinal))
            {
                FinalizeList(current, ref currentListKey);
                current = new Intent(
                    Id: trimmedStart.Substring("- id:".Length).Trim(),
                    Fields: new Dictionary<string, string> { ["id"] = trimmedStart.Substring("- id:".Length).Trim() },
                    Lists: new Dictionary<string, List<string>>());
                intents.Add(current);
                intentIndent = indent;
                continue;
            }

            if (current is null)
            {
                continue;
            }

            // List items belong to the current list key if the indent matches (intent indent + 4 typically).
            if (trimmedStart.StartsWith('-'))
            {
                if (currentListKey is not null && indent > intentIndent)
                {
                    var value = trimmedStart.Substring(1).Trim();
                    value = StripQuotes(value);
                    if (!string.IsNullOrEmpty(value))
                    {
                        current.Lists[currentListKey].Add(value);
                    }
                }
                continue;
            }

            // key: value or key:
            var colonIdx = trimmedStart.IndexOf(':');
            if (colonIdx <= 0)
            {
                continue;
            }

            // Only top-level intent fields live at (intentIndent + 2).
            if (indent != intentIndent + 2)
            {
                continue;
            }

            FinalizeList(current, ref currentListKey);
            var key = trimmedStart.Substring(0, colonIdx).Trim();
            var valueText = trimmedStart.Substring(colonIdx + 1).Trim();

            if (string.IsNullOrEmpty(valueText))
            {
                // Block scalar or block sequence follows.
                currentListKey = key;
                current.Lists[key] = new List<string>();
            }
            else
            {
                current.Fields[key] = StripQuotes(valueText);
            }
        }

        FinalizeList(current, ref currentListKey);

        return intents;

        static void FinalizeList(Intent? intent, ref string? listKey)
        {
            listKey = null;
        }

        static string StripQuotes(string v)
        {
            if (v.Length >= 2 && ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
            {
                return v.Substring(1, v.Length - 2);
            }
            return v;
        }
    }
}
