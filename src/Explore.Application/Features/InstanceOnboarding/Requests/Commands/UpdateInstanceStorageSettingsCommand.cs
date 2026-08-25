// ABOUTME: Command for updating provider-neutral instance storage settings by an instance administrator.
// ABOUTME: Persists policy, quota, delegation, and optional S3 provider configuration.

using Explore.Application.DTOs.Instance;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed record UpdateInstanceStorageSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; init; }
    public required PatchInstanceStorageSettingsDto Patch { get; init; } = new();
}
