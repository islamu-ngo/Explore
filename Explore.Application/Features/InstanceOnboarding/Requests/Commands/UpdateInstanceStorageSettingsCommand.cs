// ABOUTME: Command for updating instance-level S3 storage settings by an instance administrator.
// ABOUTME: Persists S3 configuration to SystemSetting records and invalidates the S3 config cache.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateInstanceStorageSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required InstanceStorageSettingsDto Settings { get; set; } = new();
}
