// ABOUTME: Handles admin static bundle export requests from merged offline bundle storage.
// ABOUTME: Uses the static bundle reader so exports never call Tolgee or Weblate.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Requests.Queries;
using Explore.Domain.Common.Localization;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Explore.Application.Features.Localization.Handlers.Queries;

public class ExportLocalizationBundleQueryHandler : IRequestHandler<ExportLocalizationBundleQuery, IReadOnlyDictionary<string, string>>
{
    private readonly IStaticTranslationBundleReader _bundleReader;

    public ExportLocalizationBundleQueryHandler(IStaticTranslationBundleReader bundleReader)
    {
        _bundleReader = bundleReader;
    }

    public Task<IReadOnlyDictionary<string, string>> Handle(
        ExportLocalizationBundleQuery request,
        CancellationToken cancellationToken)
    {
        if (!CultureRegistry.TryGetEntry(request.LanguageCode, out var culture))
            throw new ValidationException([
                new ValidationFailure(nameof(request.LanguageCode), "Language code is not supported."),
            ]);

        return _bundleReader.ReadBundleAsync(culture.Code, cancellationToken);
    }
}
