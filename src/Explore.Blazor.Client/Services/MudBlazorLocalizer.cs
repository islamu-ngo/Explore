// ABOUTME: Bridges MudBlazor's MudLocalizer to our ITranslationService using "mudblazor.{key}" prefix.
// ABOUTME: Enables MudBlazor component strings (data grid, pagination, dialogs) to follow user language.

using Explore.Blazor.Client.Contracts.Services;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Explore.Blazor.Client.Services;

public sealed class MudBlazorLocalizer : MudLocalizer
{
    private readonly ITranslationService _translationService;

    public MudBlazorLocalizer(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public override LocalizedString this[string key]
    {
        get
        {
            var translationKey = $"mudblazor.{key.ToLowerInvariant()}";
            var value = _translationService.T(translationKey, fallback: key);
            var resourceNotFound = string.Equals(value, key, StringComparison.Ordinal);
            return new LocalizedString(key, value, resourceNotFound);
        }
    }
}
