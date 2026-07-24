// ABOUTME: Command to update analytics governance settings from admin UI.

using Explore.Application.DTOs.Instance;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed class UpdateAnalyticsGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required Guid UserId { get; init; }
    public required PatchAnalyticsGovernanceSettingsDto Patch { get; init; }
}
