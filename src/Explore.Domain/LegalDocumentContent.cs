// ABOUTME: Owns bounded localized legal source and non-certifying template provenance.
// ABOUTME: Enforces a deterministic network-free Markdown subset before public mutation.

namespace Explore.Domain;

using System.Text;

public static class LegalDocumentContentLimits
{
    public const int MaximumDocumentsPerScope = 16;
    public const int MaximumLocalesPerDocument = 32;
    public const int MaximumMarkdownUtf8BytesPerLocale = 256 * 1024;
    public const int MaximumLinksPerLocale = 128;
    public const int MaximumPlaceholdersPerLocale = 64;
    public const int MaximumTitleLength = 200;
    public const int MaximumSummaryLength = 500;
    public const int MaximumLanguageTagLength = 35;
    public const int MaximumLinkLength = 2048;
}

public sealed record LegalDocumentTemplateProvenance
{
    private LegalDocumentTemplateProvenance(
        string templateId,
        string templateVersion,
        LegalDocumentTemplateSourceKind sourceKind,
        string licenseExpression,
        string reviewReference)
    {
        TemplateId = templateId;
        TemplateVersion = templateVersion;
        SourceKind = sourceKind;
        LicenseExpression = licenseExpression;
        ReviewReference = reviewReference;
    }

    public string TemplateId { get; }
    public string TemplateVersion { get; }
    public LegalDocumentTemplateSourceKind SourceKind { get; }
    public string LicenseExpression { get; }
    public string ReviewReference { get; }
    public bool IsLegalAdvice => false;
    public bool IsCertification => false;

    public static LegalDocumentTemplateProvenance Create(
        string templateId,
        string templateVersion,
        LegalDocumentTemplateSourceKind sourceKind,
        string licenseExpression,
        string reviewReference)
    {
        if (!Enum.IsDefined(sourceKind))
            throw new ArgumentOutOfRangeException(nameof(sourceKind));

        return new LegalDocumentTemplateProvenance(
            NormalizeRequired(templateId, 100, nameof(templateId)),
            NormalizeRequired(templateVersion, 50, nameof(templateVersion)),
            sourceKind,
            NormalizeRequired(licenseExpression, 100, nameof(licenseExpression)),
            NormalizeRequired(reviewReference, 200, nameof(reviewReference)));
    }

    private static string NormalizeRequired(
        string value,
        int maximumLength,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentOutOfRangeException(parameterName);
        return normalized;
    }
}

public sealed class LegalDocumentLocalizedSource
{
    private LegalDocumentLocalizedSource()
    {
    }

    public Guid Id { get; private set; }
    public Guid LegalDocumentVersionId { get; private set; }
    public LegalDocumentVersion? LegalDocumentVersion { get; private set; }
    public string LanguageTag { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Markdown { get; private set; } = string.Empty;
    public int Utf8ByteCount { get; private set; }
    public int LinkCount { get; private set; }
    public int PlaceholderCount { get; private set; }

    public static LegalDocumentLocalizedSource Create(
        string languageTag,
        string title,
        string summary,
        string markdown)
    {
        string normalizedLanguage = NormalizeLanguageTag(languageTag);
        string normalizedTitle = NormalizeText(
            title,
            LegalDocumentContentLimits.MaximumTitleLength,
            nameof(title));
        string normalizedSummary = NormalizeText(
            summary,
            LegalDocumentContentLimits.MaximumSummaryLength,
            nameof(summary));
        ArgumentNullException.ThrowIfNull(markdown);
        string normalizedMarkdown = markdown.Replace(
            "\r\n",
            "\n",
            StringComparison.Ordinal);
        if (normalizedMarkdown.Contains('\r', StringComparison.Ordinal))
            throw new ArgumentException("Legal Markdown must use canonical line endings.", nameof(markdown));

        int byteCount = Encoding.UTF8.GetByteCount(normalizedMarkdown);
        if (byteCount is < 1 or >
            LegalDocumentContentLimits.MaximumMarkdownUtf8BytesPerLocale)
        {
            throw new ArgumentOutOfRangeException(nameof(markdown));
        }

        MarkdownShape shape = ValidateMarkdown(normalizedMarkdown);
        return new LegalDocumentLocalizedSource
        {
            Id = Guid.CreateVersion7(),
            LanguageTag = normalizedLanguage,
            Title = normalizedTitle,
            Summary = normalizedSummary,
            Markdown = normalizedMarkdown,
            Utf8ByteCount = byteCount,
            LinkCount = shape.LinkCount,
            PlaceholderCount = shape.PlaceholderCount
        };
    }

    internal void BindVersion(Guid versionId, LegalDocumentVersion version)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(versionId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(version);
        if (LegalDocumentVersionId != Guid.Empty)
            throw new InvalidOperationException("Localized legal source is already bound.");

        LegalDocumentVersionId = versionId;
        LegalDocumentVersion = version;
    }

    private static MarkdownShape ValidateMarkdown(string markdown)
    {
        if (markdown.Contains('<', StringComparison.Ordinal)
            || markdown.Contains('>', StringComparison.Ordinal))
        {
            throw new ArgumentException("Raw HTML and autolinks are not allowed.", nameof(markdown));
        }

        if (markdown.Contains("![", StringComparison.Ordinal))
            throw new ArgumentException("Embedded resources are not allowed.", nameof(markdown));

        int linkCount = 0;
        int cursor = 0;
        while ((cursor = markdown.IndexOf("](", cursor, StringComparison.Ordinal)) >= 0)
        {
            int urlStart = cursor + 2;
            int urlEnd = markdown.IndexOf(')', urlStart);
            if (urlEnd < 0)
                throw new ArgumentException("Markdown link is incomplete.", nameof(markdown));

            string url = markdown[urlStart..urlEnd];
            ValidateLink(url);
            linkCount++;
            if (linkCount > LegalDocumentContentLimits.MaximumLinksPerLocale)
                throw new ArgumentOutOfRangeException(nameof(markdown));
            cursor = urlEnd + 1;
        }

        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        int placeholderCursor = 0;
        while (true)
        {
            int start = markdown.IndexOf(
                "{{",
                placeholderCursor,
                StringComparison.Ordinal);
            if (start < 0)
                break;

            int end = markdown.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
                throw new ArgumentException("Legal identity placeholder is incomplete.", nameof(markdown));

            string placeholder = markdown[(start + 2)..end];
            if (placeholder.Length is < 1 or > 100
                || placeholder.Any(character =>
                    !char.IsAsciiLetterOrDigit(character)
                    && character is not '_' and not '-' and not '.'))
            {
                throw new ArgumentException("Legal identity placeholder is invalid.", nameof(markdown));
            }

            placeholders.Add(placeholder);
            if (placeholders.Count >
                LegalDocumentContentLimits.MaximumPlaceholdersPerLocale)
            {
                throw new ArgumentOutOfRangeException(nameof(markdown));
            }

            placeholderCursor = end + 2;
        }

        if (markdown.IndexOf(
                "}}",
                placeholderCursor,
                StringComparison.Ordinal) >= 0)
            throw new ArgumentException("Legal identity placeholder is malformed.", nameof(markdown));

        return new MarkdownShape(linkCount, placeholders.Count);
    }

    private static void ValidateLink(string value)
    {
        if (value.Length is < 1 or > LegalDocumentContentLimits.MaximumLinkLength
            || !Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Legal Markdown link is unsafe.", nameof(value));
        }
    }

    private static string NormalizeLanguageTag(string languageTag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageTag);
        string normalized = languageTag.Trim();
        if (normalized.Length > LegalDocumentContentLimits.MaximumLanguageTagLength)
            throw new ArgumentOutOfRangeException(nameof(languageTag));

        string[] segments = normalized.Split(
            '-',
            StringSplitOptions.RemoveEmptyEntries);
        bool privateUse = segments.Length >= 2
            && string.Equals(segments[0], "x", StringComparison.OrdinalIgnoreCase);
        bool validPrimary = privateUse
            || segments.Length > 0
            && segments[0].Length is 2 or 3
            && segments[0].All(char.IsAsciiLetter);
        if (!validPrimary
            || segments.Skip(1).Any(segment =>
                segment.Length is < 1 or > 8
                || !segment.All(char.IsAsciiLetterOrDigit)))
        {
            throw new ArgumentException("Language tag is invalid.", nameof(languageTag));
        }

        return string.Join(
            '-',
            segments.Select((segment, index) =>
                index == 0 && !privateUse
                    ? segment.ToLowerInvariant()
                    : segment.Length == 2 && segment.All(char.IsAsciiLetter)
                        ? segment.ToUpperInvariant()
                        : segment.ToLowerInvariant()));
    }

    private static string NormalizeText(
        string value,
        int maximumLength,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentOutOfRangeException(parameterName);
        return normalized;
    }

    private sealed record MarkdownShape(int LinkCount, int PlaceholderCount);
}
