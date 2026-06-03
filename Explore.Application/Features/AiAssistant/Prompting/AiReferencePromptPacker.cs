// ABOUTME: Packs selected AI references into bounded prompt context blocks.
// ABOUTME: Uses explicit XML-like boundaries so model-visible reference text remains quoted context.

using Explore.Application.DTOs.Ai;

namespace Explore.Application.Features.AiAssistant.Prompting;

public sealed class AiReferencePromptPacker
{
    public const int DefaultMaxReferences = 5;
    public const int DefaultMaxCharactersPerReference = 500;
    public const int DefaultMaxTotalCharacters = 2_000;

    public string Pack(
        IReadOnlyList<AiSelectedReferenceDto> references,
        int maxReferences = DefaultMaxReferences,
        int maxCharactersPerReference = DefaultMaxCharactersPerReference,
        int maxTotalCharacters = DefaultMaxTotalCharacters)
    {
        if (references.Count == 0 || maxReferences <= 0 || maxCharactersPerReference <= 0 || maxTotalCharacters <= 0)
        {
            return string.Empty;
        }

        var blocks = new List<string>();
        var totalCharacters = 0;

        foreach (AiSelectedReferenceDto reference in references.Take(maxReferences))
        {
            string block = BuildReferenceBlock(reference, maxCharactersPerReference);
            if (block.Length == 0)
            {
                continue;
            }

            if (totalCharacters + block.Length > maxTotalCharacters)
            {
                break;
            }

            blocks.Add(block);
            totalCharacters += block.Length;
        }

        if (blocks.Count == 0)
        {
            return string.Empty;
        }

        return "<selected_references>\n" + string.Join("\n", blocks) + "\n</selected_references>";
    }

    private static string BuildReferenceBlock(AiSelectedReferenceDto reference, int maxCharacters)
    {
        string kind = Normalize(reference.Kind);
        string displayName = Normalize(reference.DisplayName);
        string summary = Normalize(reference.Summary ?? string.Empty);

        if (string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        string block = $"<reference kind=\"{EscapeAttribute(kind)}\" id=\"{reference.ReferenceId}\">\n<title>{EscapeText(displayName)}</title>\n<summary>{EscapeText(summary)}</summary>\n</reference>";
        return Truncate(block, maxCharacters);
    }

    private static string Normalize(string value) => value.Trim();

    private static string Truncate(string value, int maxCharacters)
    {
        if (value.Length <= maxCharacters)
        {
            return value;
        }

        return value[..maxCharacters].TrimEnd();
    }

    private static string EscapeAttribute(string value) => EscapeText(value).Replace("\"", "&quot;");

    private static string EscapeText(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}
