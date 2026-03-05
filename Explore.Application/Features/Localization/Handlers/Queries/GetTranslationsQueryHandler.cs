// ABOUTME: Handler for GetTranslationsQuery that resolves all translations for a language.
// ABOUTME: Uses ITranslationManagementProvider to export translations, returns key-value dictionary.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Localization.Handlers.Queries;

public class GetTranslationsQueryHandler : IRequestHandler<GetTranslationsQuery, Dictionary<string, string>>
{
    private readonly ITranslationManagementProvider _translationProvider;

    public GetTranslationsQueryHandler(ITranslationManagementProvider translationProvider)
    {
        _translationProvider = translationProvider;
    }

    public async Task<Dictionary<string, string>> Handle(GetTranslationsQuery request, CancellationToken cancellationToken)
    {
        var exports = await _translationProvider.ExportTranslationsAsync(request.LanguageCode, cancellationToken);
        return exports.ToDictionary(e => e.KeyName, e => e.Value);
    }
}
