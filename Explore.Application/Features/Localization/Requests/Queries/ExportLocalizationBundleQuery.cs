// ABOUTME: Query for exporting the current merged static localization bundle for one language.
// ABOUTME: Reads offline bundle state only, avoiding live TMS provider calls.

using MediatR;

namespace Explore.Application.Features.Localization.Requests.Queries;

public class ExportLocalizationBundleQuery : IRequest<IReadOnlyDictionary<string, string>>
{
    public required string LanguageCode { get; init; }
}
