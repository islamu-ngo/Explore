// ABOUTME: Command contract for completing first-run instance onboarding.
// ABOUTME: Assigns first instance admin and persists selected governance settings.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class CompleteInstanceOnboardingCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required InstanceGovernanceSettingsDto Settings { get; set; } = new();
}
