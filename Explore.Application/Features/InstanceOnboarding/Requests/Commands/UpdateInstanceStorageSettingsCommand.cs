// ABOUTME: Command for updating provider-neutral instance storage settings by an instance administrator.
// ABOUTME: Persists policy, quota, delegation, and optional S3 provider configuration.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateInstanceStorageSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required InstanceStorageSettingsDto Settings { get; set; } = new();
}
