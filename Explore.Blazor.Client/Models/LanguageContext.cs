// ABOUTME: Immutable model representing the current language state for the Blazor application.
// ABOUTME: Provided as a CascadingValue by LanguageProvider for all child components.

namespace Explore.Blazor.Client.Models;

/// <summary>
/// Represents the current language context for the application.
/// Provided as a CascadingValue by LanguageProvider.
/// </summary>
public class LanguageContext
{
    /// <summary>ISO 639-1 language code (e.g., "en", "fr", "ar").</summary>
    public string LanguageCode { get; set; } = "en";

    /// <summary>Whether the current language is right-to-left.</summary>
    public bool IsRtl { get; set; }

    /// <summary>Display name of the current language (e.g., "English", "Français", "العربية").</summary>
    public string LanguageName { get; set; } = "English";

    /// <summary>Emoji flag representing the language.</summary>
    public string Flag { get; set; } = "🇺🇸";

    /// <summary>RTL language codes.</summary>
    private static readonly HashSet<string> RtlLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "he", "fa", "ur"
    };

    /// <summary>Well-known language flags.</summary>
    private static readonly Dictionary<string, string> LanguageFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "🇺🇸",
        ["fr"] = "🇫🇷",
        ["ar"] = "🇸🇦",
        ["he"] = "🇮🇱",
        ["fa"] = "🇮🇷",
        ["ur"] = "🇵🇰",
        ["tr"] = "🇹🇷",
        ["id"] = "🇮🇩",
        ["ms"] = "🇲🇾",
        ["de"] = "🇩🇪",
        ["es"] = "🇪🇸"
    };

    /// <summary>Well-known language display names.</summary>
    private static readonly Dictionary<string, string> LanguageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "English",
        ["fr"] = "Français",
        ["ar"] = "العربية",
        ["he"] = "עברית",
        ["fa"] = "فارسی",
        ["ur"] = "اردو",
        ["tr"] = "Türkçe",
        ["id"] = "Bahasa Indonesia",
        ["ms"] = "Bahasa Melayu",
        ["de"] = "Deutsch",
        ["es"] = "Español"
    };

    /// <summary>Creates a LanguageContext for the given language code.</summary>
    public static LanguageContext ForLanguage(string languageCode)
    {
        var code = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
        return new LanguageContext
        {
            LanguageCode = code,
            IsRtl = RtlLanguages.Contains(code),
            LanguageName = LanguageNames.GetValueOrDefault(code, code.ToUpperInvariant()),
            Flag = LanguageFlags.GetValueOrDefault(code, "🌐")
        };
    }
}
