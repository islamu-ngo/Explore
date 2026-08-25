// ABOUTME: Command contract for updating authorization provider configuration after onboarding.
// ABOUTME: Carries the current user identity for instance-admin authorization checks in admin settings.

using Explore.Application.DTOs.Instance;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed record UpdateAuthorizationProviderConfigurationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; init; }
    public required PatchAuthorizationProviderConfigurationDto Patch { get; init; } = new();
}
