// ABOUTME: Shared helpers for AI-context architecture tests.
// ABOUTME: Locates repo root, parses markdown frontmatter and H2 sections used by AgentContext*Tests.

namespace Event.Architecture.Tests;

using System.Text.RegularExpressions;

internal static class ContextSystemHelpers
{
    private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

    public static string RepoRoot => RepoRootLazy.Value;

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var agents = Path.Combine(dir.FullName, "AGENTS.md");
            var claude = Path.Combine(dir.FullName, "AGENTS.md");
            if (File.Exists(agents) && File.Exists(claude))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Repository root not found: neither AGENTS.md nor AGENTS.md discovered while walking up from test output directory.");
    }

    public static string RepoPath(params string[] segments) =>
        Path.Combine(new[] { RepoRoot }.Concat(segments).ToArray());

    public static (Dictionary<string, string> Frontmatter, string Body) ParseMarkdown(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        int cursor = 0;

        // Skip leading HTML comments (e.g. ABOUTME) and blank lines before frontmatter.
        while (cursor < lines.Length)
        {
            var line = lines[cursor].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                cursor++;
                continue;
            }
            if (line.StartsWith("<!--", StringComparison.Ordinal))
            {
                while (cursor < lines.Length && !lines[cursor].Contains("-->", StringComparison.Ordinal))
                {
                    cursor++;
                }
                if (cursor < lines.Length) { cursor++; }
                continue;
            }
            break;
        }

        if (cursor >= lines.Length || lines[cursor].Trim() != "---")
        {
            return (new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), content);
        }

        var fmStart = cursor + 1;
        var fmEnd = -1;
        for (int i = fmStart; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                fmEnd = i;
                break;
            }
        }
        if (fmEnd < 0)
        {
            return (new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), content);
        }

        var fm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentKey = null;
        var currentValueLines = new List<string>();

        void Flush()
        {
            if (currentKey is not null)
            {
                fm[currentKey] = string.Join("\n", currentValueLines).TrimEnd();
            }
            currentKey = null;
            currentValueLines.Clear();
        }

        for (int i = fmStart; i < fmEnd; i++)
        {
            var raw = lines[i];
            // A top-level key must start at column 0 (no leading whitespace).
            var match = Regex.Match(raw, "^([A-Za-z_][A-Za-z0-9_-]*):\\s*(.*)$");
            if (match.Success && !raw.StartsWith(" ", StringComparison.Ordinal) && !raw.StartsWith("\t", StringComparison.Ordinal))
            {
                Flush();
                currentKey = match.Groups[1].Value;
                var valuePart = match.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(valuePart))
                {
                    currentValueLines.Add(valuePart);
                }
            }
            else if (currentKey is not null)
            {
                currentValueLines.Add(raw);
            }
        }
        Flush();

        var body = string.Join("\n", lines.Skip(fmEnd + 1));
        return (fm, body);
    }

    public static List<string> ExtractH2Sections(string body)
    {
        var result = new List<string>();
        foreach (var rawLine in body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("## ", StringComparison.Ordinal) && !line.StartsWith("### ", StringComparison.Ordinal))
            {
                result.Add(line[3..].Trim());
            }
        }
        return result;
    }

    public static int CountLines(string path) =>
        File.ReadAllText(path).Replace("\r\n", "\n").Split('\n').Length;

    public static IEnumerable<(string Text, string Target, int LineNumber)> ExtractMarkdownLinks(string content)
    {
        var pattern = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        var lines = content.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match match in pattern.Matches(lines[i]))
            {
                var target = match.Groups[2].Value.Trim();
                if (string.IsNullOrEmpty(target)) { continue; }
                if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    target.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }
                yield return (match.Groups[1].Value, target, i + 1);
            }
        }
    }

    public static string ResolveLinkTarget(string markdownFilePath, string target)
    {
        var hashIdx = target.IndexOf('#', StringComparison.Ordinal);
        if (hashIdx >= 0)
        {
            target = target[..hashIdx];
        }
        if (string.IsNullOrEmpty(target))
        {
            return markdownFilePath;
        }

        var dir = Path.GetDirectoryName(markdownFilePath) ?? RepoRoot;
        // Leading slash means repo-root-relative per docs/index.md convention.
        if (target.StartsWith('/'))
        {
            return Path.GetFullPath(Path.Combine(RepoRoot, target.TrimStart('/')));
        }
        return Path.GetFullPath(Path.Combine(dir, target));
    }
}
