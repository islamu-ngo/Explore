// ABOUTME: Command wrapping UpdateLocalizationGovernanceDto for MediatR dispatch.
// ABOUTME: Persists 9 governance keys atomically and invalidates the translation config cache.

using Explore.Application.DTOs.Localization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Localization.Requests.Commands;

public sealed record UpdateLocalizationGovernanceCommand : IRequest<BaseCommandResponse<Guid>>
{
    public UpdateLocalizationGovernanceDto Dto { get; init; } = new();
}
