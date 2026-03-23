// ABOUTME: Query for retrieving the authenticated user's effective appearance preferences.
// ABOUTME: Used by the API/BFF runtime seam to resolve theme mode through the hierarchical settings engine.

namespace Explore.Application.Features.Appearance.Requests.Queries;

using Explore.Application.DTOs.Appearance;
using MediatR;

public class GetCurrentUserAppearancePreferencesQuery : IRequest<UserAppearancePreferencesDto>
{
}
