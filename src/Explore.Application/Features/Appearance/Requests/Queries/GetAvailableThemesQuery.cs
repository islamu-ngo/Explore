// ABOUTME: Query request for retrieving active platform and tenant themes available to the current tenant.
// ABOUTME: Forms the first application-facing read seam on top of the new UiTheme repository.

namespace Explore.Application.Features.Appearance.Requests.Queries;

using Explore.Application.DTOs.Appearance;
using MediatR;

public class GetAvailableThemesQuery : IRequest<IReadOnlyList<AvailableThemeDto>>
{
}
