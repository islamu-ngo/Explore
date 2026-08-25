// ABOUTME: Command contract for explicitly publishing the authorization policy package.
// ABOUTME: Keeps onboarding/admin flows provider-neutral while Infrastructure owns package publishing.

using Explore.Application.Responses;
using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed record SyncAuthorizationPolicyPackageCommand : IRequest<BaseCommandResponse<Guid>>
{
    public AuthorizationPolicyPackageSyncRequestDto Request { get; init; } = new();
}
