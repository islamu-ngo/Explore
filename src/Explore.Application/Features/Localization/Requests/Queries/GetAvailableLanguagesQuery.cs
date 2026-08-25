// ABOUTME: MediatR query to retrieve the list of available translation languages.
// ABOUTME: Returns language codes from the active TMS provider or offline bundles.

using MediatR;

namespace Explore.Application.Features.Localization.Requests.Queries;

public sealed record GetAvailableLanguagesQuery : IRequest<List<string>>
{
}
