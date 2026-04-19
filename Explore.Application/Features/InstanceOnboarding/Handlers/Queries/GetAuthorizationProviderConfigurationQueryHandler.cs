// ABOUTME: Query handler for authorization provider configuration used in setup and admin flows.
// ABOUTME: Auto-selects Cerbos only when no explicit configuration exists and the detected endpoint verifies successfully.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public class GetAuthorizationProviderConfigurationQueryHandler : IRequestHandler<GetAuthorizationProviderConfigurationQuery, AuthorizationProviderConfigurationDto>
{
    private readonly IAuthorizationProviderConfigurationService _configurationService;

    public GetAuthorizationProviderConfigurationQueryHandler(IAuthorizationProviderConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<AuthorizationProviderConfigurationDto> Handle(GetAuthorizationProviderConfigurationQuery request, CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.ReadConfigurationAsync();
        var isConfigured = await _configurationService.IsConfiguredAsync();

        if (string.IsNullOrWhiteSpace(configuration.CerbosGrpcEndpoint))
        {
            return configuration;
        }

        configuration.CerbosEndpointVerified = await _configurationService.VerifyCerbosEndpointAsync(
            configuration.CerbosGrpcEndpoint,
            cancellationToken);

        if (!isConfigured &&
            configuration.CerbosDetectedFromEnvironment &&
            configuration.CerbosEndpointVerified)
        {
            configuration.Provider = "cerbos";
        }

        return configuration;
    }
}
