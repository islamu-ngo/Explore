// ABOUTME: Command contract for runtime updates to instance governance settings.
// ABOUTME: Used by instance administrators to change deployment and policy settings after onboarding.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateInstanceGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required InstanceGovernanceSettingsDto Settings { get; set; } = new();
}
