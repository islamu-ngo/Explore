// ABOUTME: Request body for importing a flat static localization bundle for one language.
// ABOUTME: Carries only translation keys and values, never TMS provider credentials.

namespace Explore.Application.DTOs.Localization;

public class ImportLocalizationBundleDto
{
    public string LanguageCode { get; set; } = string.Empty;

    public Dictionary<string, string> Translations { get; set; } = [];
}
