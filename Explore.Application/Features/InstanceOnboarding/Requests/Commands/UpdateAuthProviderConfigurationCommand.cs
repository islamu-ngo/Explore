// ABOUTME: Command contract for updating auth provider configuration after onboarding.
// ABOUTME: Carries current user identity for instance-admin authorization and lockout safety checks.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateAuthProviderConfigurationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required AuthProviderConfigurationDto Configuration { get; set; } = new();
}
