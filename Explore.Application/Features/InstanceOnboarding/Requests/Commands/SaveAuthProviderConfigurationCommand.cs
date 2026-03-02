// ABOUTME: Command contract for saving auth provider configuration during instance setup.
// ABOUTME: Protected by setup token (not [Authorize]) since it runs before authentication is available.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class SaveAuthProviderConfigurationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required AuthProviderConfigurationDto Configuration { get; set; } = new();
}
