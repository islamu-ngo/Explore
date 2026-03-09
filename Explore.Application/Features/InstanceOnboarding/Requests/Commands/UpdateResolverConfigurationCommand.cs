// ABOUTME: Command contract for updating instance-level tenant resolver configuration.
// ABOUTME: Keeps resolver toggles and path-prefix settings isolated from general governance updates.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateResolverConfigurationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }

    public required ResolverConfigurationDto Configuration { get; set; } = new();
}
