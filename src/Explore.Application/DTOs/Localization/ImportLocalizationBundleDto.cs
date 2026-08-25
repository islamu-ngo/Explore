// ABOUTME: Request body for importing a flat static localization bundle for one language.
// ABOUTME: Carries only translation keys and values, never TMS provider credentials.

namespace Explore.Application.DTOs.Localization;

public sealed record ImportLocalizationBundleDto
{
    public string LanguageCode { get; init; } = string.Empty;

    public Dictionary<string, string> Translations { get; init; } = [];
}
