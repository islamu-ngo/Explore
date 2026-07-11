// ABOUTME: Shared validation for flat localization bundle JSON read from embedded resources or writable storage.
// ABOUTME: Keeps offline bundle keys aligned with TMS key shapes before providers cache or persist them.

using System.Text.Json;

namespace Explore.Infrastructure.Localization;

internal static class BundleSchema
{
    public static SortedDictionary<string, string> Read(Stream stream)
    {
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Localization bundle root must be a JSON object.");
        }

        var translations = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            ValidateKey(property.Name);
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new JsonException($"Localization bundle value for '{property.Name}' must be a string.");
            }

            translations[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return translations;
    }

    public static SortedDictionary<string, string> ValidateAndSort(IReadOnlyDictionary<string, string> translations)
    {
        var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in translations)
        {
            ValidateKey(key);
            sorted[key] = value ?? string.Empty;
        }

        return sorted;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key != key.Trim() || key.Any(char.IsWhiteSpace) || !HasAllowedPrefix(key) || key.Split('.').Any(segment => segment.Length == 0))
        {
            throw new JsonException($"Localization bundle key '{key}' must start with 'ui.' or 'lookup.' and use non-empty dot-separated segments without whitespace.");
        }
    }

    private static bool HasAllowedPrefix(string key) =>
        key.StartsWith("ui.", StringComparison.Ordinal) ||
        key.StartsWith("lookup.", StringComparison.Ordinal);
}
