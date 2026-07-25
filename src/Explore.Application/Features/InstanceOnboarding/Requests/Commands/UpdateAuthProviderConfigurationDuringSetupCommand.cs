// ABOUTME: Setup-authorized command for updating authentication provider configuration before an admin exists.
// ABOUTME: Carries only the provider patch; setup-secret material remains at the API authentication boundary.

using Explore.Application.DTOs.Instance;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class UpdateAuthProviderConfigurationDuringSetupCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required PatchAuthProviderConfigurationDto Patch { get; set; } = new();
}
