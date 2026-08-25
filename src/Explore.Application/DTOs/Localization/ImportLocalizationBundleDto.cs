// ABOUTME: Request body for importing a flat static localization bundle for one language.
// ABOUTME: Carries only translation keys and values, never TMS provider credentials.

using System.Collections.ObjectModel;

namespace Explore.Application.DTOs.Localization;

public sealed record ImportLocalizationBundleDto
{
    private IReadOnlyDictionary<string, string> _translations =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    public string LanguageCode { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Translations
    {
        get => _translations;
        init => _translations = value is null
            ? null!
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(value, StringComparer.Ordinal));
    }
}
