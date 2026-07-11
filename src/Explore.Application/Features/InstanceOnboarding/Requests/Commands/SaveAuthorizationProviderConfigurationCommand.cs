// ABOUTME: Command contract for saving authorization provider configuration during instance setup.
// ABOUTME: Protected by setup token (not [Authorize]) since it runs before authentication is available.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class SaveAuthorizationProviderConfigurationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required AuthorizationProviderConfigurationDto Configuration { get; set; } = new();
}
