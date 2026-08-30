// ABOUTME: Parses and renders the single constrained Markdown grammar used by legal documents.
// ABOUTME: Produces deterministic encoded HTML without I/O and value-safe readiness diagnostics.

namespace Explore.Domain;

using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;

public static class LegalMarkdownDiagnosticCodes
{
    public const string IdentityUnresolved = "legal_markdown_identity_unresolved";
    public const string LinkTextWeak = "legal_markdown_link_text_weak";
}

public sealed class LegalMarkdownDiagnostic
{
    internal LegalMarkdownDiagnostic(string code, string? subject)
    {
        Code = code;
        Subject = subject;
    }

    public string Code { get; }
    public string? Subject { get; }
}

public sealed class LegalMarkdownInspection
{
    internal LegalMarkdownInspection(
        int linkCount,
        int placeholderCount,
        ImmutableArray<string> linkTargets)
    {
        LinkCount = linkCount;
        PlaceholderCount = placeholderCount;
        LinkTargets = linkTargets;
    }

    public int LinkCount { get; }
    public int PlaceholderCount { get; }
    public ImmutableArray<string> LinkTargets { get; }
}

public sealed class LegalMarkdownRenderResult
{
    internal LegalMarkdownRenderResult(
        bool isReady,
        string html,
        ImmutableArray<string> linkTargets,
        ImmutableArray<LegalMarkdownDiagnostic> diagnostics)
    {
        IsReady = isReady;
        Html = html;
        LinkTargets = linkTargets;
        Diagnostics = diagnostics;
    }

    public bool IsReady { get; }
    public string Html { get; }
    public ImmutableArray<string> LinkTargets { get; }
    public ImmutableArray<LegalMarkdownDiagnostic> Diagnostics { get; }
}

public static class LegalMarkdownContract
{
    private const int MaximumIdentityValueLength = 500;

    private static readonly HashSet<string> WeakLinkLabels =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "click here",
            "here",
            "learn more",
            "link",
            "read more"
        };

    public static LegalMarkdownInspection Inspect(string markdown)
    {
        ParseResult result = Parse(markdown, identityValues: null, render: false);
        return new LegalMarkdownInspection(
            result.LinkTargets.Length,
            result.Placeholders.Length,
            result.LinkTargets);
    }

    public static LegalMarkdownRenderResult Render(
        string markdown,
        IReadOnlyDictionary<string, string> identityValues)
    {
        ArgumentNullException.ThrowIfNull(identityValues);
        foreach ((string key, string value) in identityValues)
        {
            ValidatePlaceholder(key, nameof(identityValues));
            if (string.IsNullOrWhiteSpace(value)
                || value.Length > MaximumIdentityValueLength)
            {
                throw new ArgumentException(
                    "Legal identity values must be nonblank and bounded.",
                    nameof(identityValues));
            }
        }

        ParseResult result = Parse(markdown, identityValues, render: true);
        bool ready = result.Diagnostics.All(diagnostic =>
            !string.Equals(
                diagnostic.Code,
                LegalMarkdownDiagnosticCodes.IdentityUnresolved,
                StringComparison.Ordinal));
        return new LegalMarkdownRenderResult(
            ready,
            ready ? result.Html : string.Empty,
            result.LinkTargets,
            result.Diagnostics);
    }

    internal static void ValidateLink(string value)
    {
        if (value.Length is < 1 or > LegalDocumentContentLimits.MaximumLinkLength
            || !Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host)
            || uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
            || string.Equals(uri.DnsSafeHost, "localhost", StringComparison.OrdinalIgnoreCase)
            || uri.DnsSafeHost.EndsWith(
                ".localhost",
                StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Legal Markdown link is unsafe.", nameof(value));
        }
    }

    private static ParseResult Parse(
        string markdown,
        IReadOnlyDictionary<string, string>? identityValues,
        bool render)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        if (markdown.Length == 0)
            throw new ArgumentException("Legal Markdown is required.", nameof(markdown));
        if (markdown.Contains('\r', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Legal Markdown must use canonical line endings.",
                nameof(markdown));
        }

        if (markdown.Contains('<', StringComparison.Ordinal)
            || markdown.Contains('>', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Raw HTML and autolinks are not allowed.",
                nameof(markdown));
        }

        if (markdown.Contains("![", StringComparison.Ordinal))
            throw new ArgumentException("Embedded resources are not allowed.", nameof(markdown));
        if (markdown.Contains("```", StringComparison.Ordinal)
            || markdown.Contains("~~~", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Executable or fenced content is not allowed.",
                nameof(markdown));
        }

        string[] lines = markdown.Split('\n');
        var html = new StringBuilder();
        var links = ImmutableArray.CreateBuilder<string>();
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        var diagnostics = ImmutableArray.CreateBuilder<LegalMarkdownDiagnostic>();
        int previousHeadingLevel = 0;
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                index++;
                continue;
            }

            if (TryReadHeading(line, out int headingLevel, out string heading))
            {
                if (headingLevel > 5
                    || previousHeadingLevel == 0 && headingLevel > 1
                    || previousHeadingLevel > 0
                        && headingLevel > previousHeadingLevel + 1)
                {
                    throw new ArgumentException(
                        "Legal Markdown heading order is inaccessible.",
                        nameof(markdown));
                }

                previousHeadingLevel = headingLevel;
                if (render)
                {
                    int htmlLevel = headingLevel + 1;
                    html.Append('<').Append('h').Append(htmlLevel).Append('>');
                    AppendInline(
                        heading,
                        identityValues,
                        html,
                        links,
                        placeholders,
                        diagnostics,
                        render);
                    html.Append("</h").Append(htmlLevel).Append(">\n");
                }
                else
                {
                    AppendInline(
                        heading,
                        identityValues,
                        html,
                        links,
                        placeholders,
                        diagnostics,
                        render);
                }

                index++;
                continue;
            }

            if (line.StartsWith('#'))
            {
                throw new ArgumentException(
                    "Legal Markdown heading syntax is invalid.",
                    nameof(markdown));
            }

            if (TryReadUnorderedItem(line, out string unorderedItem))
            {
                if (render)
                    html.Append("<ul>\n");
                do
                {
                    if (render)
                        html.Append("<li>");
                    AppendInline(
                        unorderedItem,
                        identityValues,
                        html,
                        links,
                        placeholders,
                        diagnostics,
                        render);
                    if (render)
                        html.Append("</li>\n");
                    index++;
                }
                while (index < lines.Length
                    && TryReadUnorderedItem(lines[index], out unorderedItem));
                if (render)
                    html.Append("</ul>\n");
                continue;
            }

            if (TryReadOrderedItem(line, out string orderedItem))
            {
                if (render)
                    html.Append("<ol>\n");
                do
                {
                    if (render)
                        html.Append("<li>");
                    AppendInline(
                        orderedItem,
                        identityValues,
                        html,
                        links,
                        placeholders,
                        diagnostics,
                        render);
                    if (render)
                        html.Append("</li>\n");
                    index++;
                }
                while (index < lines.Length
                    && TryReadOrderedItem(lines[index], out orderedItem));
                if (render)
                    html.Append("</ol>\n");
                continue;
            }

            var paragraph = new StringBuilder(line.Trim());
            index++;
            while (index < lines.Length
                && !string.IsNullOrWhiteSpace(lines[index])
                && !IsBlockStart(lines[index]))
            {
                paragraph.Append(' ').Append(lines[index].Trim());
                index++;
            }

            if (render)
                html.Append("<p>");
            AppendInline(
                paragraph.ToString(),
                identityValues,
                html,
                links,
                placeholders,
                diagnostics,
                render);
            if (render)
                html.Append("</p>\n");
        }

        if (links.Count > LegalDocumentContentLimits.MaximumLinksPerLocale)
            throw new ArgumentOutOfRangeException(nameof(markdown));
        if (placeholders.Count >
            LegalDocumentContentLimits.MaximumPlaceholdersPerLocale)
        {
            throw new ArgumentOutOfRangeException(nameof(markdown));
        }

        return new ParseResult(
            html.ToString(),
            links.ToImmutable(),
            placeholders.Order(StringComparer.Ordinal).ToImmutableArray(),
            diagnostics.ToImmutable());
    }

    private static void AppendInline(
        string value,
        IReadOnlyDictionary<string, string>? identityValues,
        StringBuilder html,
        ImmutableArray<string>.Builder links,
        HashSet<string> placeholders,
        ImmutableArray<LegalMarkdownDiagnostic>.Builder diagnostics,
        bool render)
    {
        int cursor = 0;
        while (cursor < value.Length)
        {
            if (Matches(value, cursor, "{{"))
            {
                int end = value.IndexOf("}}", cursor + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    throw new ArgumentException(
                        "Legal identity placeholder is incomplete.",
                        nameof(value));
                }

                string placeholder = value[(cursor + 2)..end];
                ValidatePlaceholder(placeholder, nameof(value));
                placeholders.Add(placeholder);
                if (render)
                {
                    if (identityValues is not null
                        && identityValues.TryGetValue(placeholder, out string? resolved)
                        && !string.IsNullOrWhiteSpace(resolved))
                    {
                        html.Append(HtmlEncoder.Default.Encode(resolved));
                    }
                    else
                    {
                        diagnostics.Add(new LegalMarkdownDiagnostic(
                            LegalMarkdownDiagnosticCodes.IdentityUnresolved,
                            placeholder));
                    }
                }

                cursor = end + 2;
                continue;
            }

            if (Matches(value, cursor, "}}")
                || value[cursor] is '{' or '}')
            {
                throw new ArgumentException(
                    "Legal identity placeholder is malformed.",
                    nameof(value));
            }

            if (value[cursor] == '[')
            {
                int labelEnd = value.IndexOf("](", cursor + 1, StringComparison.Ordinal);
                int urlEnd = labelEnd < 0
                    ? -1
                    : value.IndexOf(')', labelEnd + 2);
                if (labelEnd < 0 || urlEnd < 0)
                    throw new ArgumentException("Markdown link is incomplete.", nameof(value));

                string label = value[(cursor + 1)..labelEnd];
                string target = value[(labelEnd + 2)..urlEnd];
                if (string.IsNullOrWhiteSpace(label)
                    || label.Contains('[', StringComparison.Ordinal)
                    || label.Contains(']', StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Markdown link text is invalid.",
                        nameof(value));
                }

                ValidateLink(target);
                links.Add(target);
                if (IsWeakLinkLabel(label))
                {
                    diagnostics.Add(new LegalMarkdownDiagnostic(
                        LegalMarkdownDiagnosticCodes.LinkTextWeak,
                        subject: null));
                }

                if (render)
                {
                    html.Append("<a href=\"")
                        .Append(HtmlEncoder.Default.Encode(target))
                        .Append("\" rel=\"noopener noreferrer\">");
                    AppendInline(
                        label,
                        identityValues,
                        html,
                        links: ImmutableArray.CreateBuilder<string>(),
                        placeholders,
                        diagnostics,
                        render);
                    html.Append("</a>");
                }

                cursor = urlEnd + 1;
                continue;
            }

            if (value[cursor] == ']')
            {
                throw new ArgumentException(
                    "Markdown punctuation is malformed.",
                    nameof(value));
            }

            if (Matches(value, cursor, "**"))
            {
                int end = value.IndexOf("**", cursor + 2, StringComparison.Ordinal);
                if (end <= cursor + 2)
                    throw new ArgumentException("Strong emphasis is incomplete.", nameof(value));
                if (render)
                    html.Append("<strong>");
                AppendInline(
                    value[(cursor + 2)..end],
                    identityValues,
                    html,
                    links,
                    placeholders,
                    diagnostics,
                    render);
                if (render)
                    html.Append("</strong>");
                cursor = end + 2;
                continue;
            }

            if (value[cursor] == '*')
            {
                int end = value.IndexOf('*', cursor + 1);
                if (end <= cursor + 1)
                    throw new ArgumentException("Emphasis is incomplete.", nameof(value));
                if (render)
                    html.Append("<em>");
                AppendInline(
                    value[(cursor + 1)..end],
                    identityValues,
                    html,
                    links,
                    placeholders,
                    diagnostics,
                    render);
                if (render)
                    html.Append("</em>");
                cursor = end + 1;
                continue;
            }

            if (value[cursor] == '`')
            {
                int end = value.IndexOf('`', cursor + 1);
                if (end <= cursor + 1)
                    throw new ArgumentException("Inline code is incomplete.", nameof(value));
                if (render)
                {
                    html.Append("<code>")
                        .Append(HtmlEncoder.Default.Encode(value[(cursor + 1)..end]))
                        .Append("</code>");
                }

                cursor = end + 1;
                continue;
            }

            if (value[cursor] == '\\')
            {
                if (cursor + 1 >= value.Length)
                    throw new ArgumentException("Markdown escape is incomplete.", nameof(value));
                if (render)
                    html.Append(HtmlEncoder.Default.Encode(value[cursor + 1].ToString()));
                cursor += 2;
                continue;
            }

            int textEnd = cursor + 1;
            while (textEnd < value.Length
                && value[textEnd] is not '[' and not ']'
                    and not '*' and not '`' and not '\\' and not '{' and not '}')
            {
                textEnd++;
            }

            if (render)
                html.Append(HtmlEncoder.Default.Encode(value[cursor..textEnd]));
            cursor = textEnd;
        }
    }

    private static bool TryReadHeading(
        string line,
        out int level,
        out string heading)
    {
        level = 0;
        while (level < line.Length && line[level] == '#')
            level++;
        bool valid = level > 0
            && level < line.Length
            && line[level] == ' '
            && !string.IsNullOrWhiteSpace(line[(level + 1)..]);
        heading = valid ? line[(level + 1)..].Trim() : string.Empty;
        return valid;
    }

    private static bool TryReadUnorderedItem(string line, out string item)
    {
        bool valid = line.StartsWith("- ", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(line[2..]);
        item = valid ? line[2..].Trim() : string.Empty;
        return valid;
    }

    private static bool TryReadOrderedItem(string line, out string item)
    {
        int cursor = 0;
        while (cursor < line.Length && char.IsAsciiDigit(line[cursor]))
            cursor++;
        bool valid = cursor > 0
            && cursor + 1 < line.Length
            && line[cursor] == '.'
            && line[cursor + 1] == ' '
            && !string.IsNullOrWhiteSpace(line[(cursor + 2)..]);
        item = valid ? line[(cursor + 2)..].Trim() : string.Empty;
        return valid;
    }

    private static bool IsBlockStart(string line) =>
        TryReadHeading(line, out _, out _)
        || TryReadUnorderedItem(line, out _)
        || TryReadOrderedItem(line, out _);

    private static bool IsWeakLinkLabel(string label)
    {
        string normalized = label.Trim();
        return WeakLinkLabels.Contains(normalized)
            || Uri.TryCreate(normalized, UriKind.Absolute, out _);
    }

    private static void ValidatePlaceholder(
        string placeholder,
        string parameterName)
    {
        if (placeholder.Length is < 1 or > 100
            || placeholder.Any(character =>
                !char.IsAsciiLetterOrDigit(character)
                && character is not '_' and not '-' and not '.'))
        {
            throw new ArgumentException(
                "Legal identity placeholder is invalid.",
                parameterName);
        }
    }

    private static bool Matches(string value, int start, string token) =>
        start + token.Length <= value.Length
        && value.AsSpan(start, token.Length).SequenceEqual(token);

    private sealed record ParseResult(
        string Html,
        ImmutableArray<string> LinkTargets,
        ImmutableArray<string> Placeholders,
        ImmutableArray<LegalMarkdownDiagnostic> Diagnostics);
}
