// ABOUTME: MediatR query to retrieve all translations for a given language code.
// ABOUTME: Returns a dictionary of translation key → translated value pairs.

using MediatR;

namespace Explore.Application.Features.Localization.Requests.Queries;

public class GetTranslationsQuery : IRequest<Dictionary<string, string>>
{
    public required string LanguageCode { get; set; }
}
