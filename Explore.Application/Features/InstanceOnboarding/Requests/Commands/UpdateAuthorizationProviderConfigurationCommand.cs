// ABOUTME: Command contract for updating authorization provider configuration after onboarding.
// ABOUTME: Carries the current user identity for instance-admin authorization checks in admin settings.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateAuthorizationProviderConfigurationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required AuthorizationProviderConfigurationDto Configuration { get; set; } = new();
}
