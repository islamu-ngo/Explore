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

        // Auto-select Cerbos when already persisted as provider or detected from environment.
        // Verification is performed on-demand via the /verify endpoint, not during page load,
        // to avoid blocking the response with a gRPC health check.
        if (!isConfigured && configuration.CerbosDetectedFromEnvironment)
        {
            configuration.Provider = "cerbos";
        }

        return configuration;
    }
}
