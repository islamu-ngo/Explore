// ABOUTME: Command contract for completing tenant onboarding policy questionnaire.
// ABOUTME: Persists tenant policy overrides and marks tenant onboarding as completed.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Requests.Commands;

public class CompleteTenantOnboardingCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required TenantPolicySettingsDto Settings { get; set; } = new();
}
