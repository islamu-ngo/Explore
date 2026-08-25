// ABOUTME: Command for updating the authenticated user's appearance preferences.
// ABOUTME: Persists sparse user overrides while allowing inherited values to flow from parent scopes.

namespace Explore.Application.Features.Appearance.Requests.Commands;

using Explore.Application.DTOs.Appearance;
using Explore.Application.Responses;
using MediatR;

public sealed record UpdateCurrentUserAppearancePreferencesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateUserAppearancePreferencesDto Preferences { get; init; }
}
