// ABOUTME: Command contract for completing tenant onboarding policy questionnaire.
// ABOUTME: Persists tenant policy overrides and marks tenant onboarding as completed.

using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Requests.Commands;

public sealed record CompleteTenantOnboardingCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; init; }
    public required UpdateTenantPolicyRequest Settings { get; init; } = new();
}
