// ABOUTME: Command contract for persisting tenant onboarding step progress without completing onboarding.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Requests.Commands;

public class SaveTenantOnboardingStepCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string[] CompletedSteps { get; set; } = Array.Empty<string>();
}
