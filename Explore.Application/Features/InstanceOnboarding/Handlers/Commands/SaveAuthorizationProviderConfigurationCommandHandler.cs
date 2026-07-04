// ABOUTME: Handles saving authorization provider configuration during setup before authentication is available.
// ABOUTME: Validates the request, verifies Cerbos reachability when selected, and persists the chosen provider.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class SaveAuthorizationProviderConfigurationCommandHandler : IRequestHandler<SaveAuthorizationProviderConfigurationCommand, BaseCommandResponse<Guid>>
{
    private readonly IAuthorizationProviderConfigurationService _configurationService;
    private readonly ILogger<SaveAuthorizationProviderConfigurationCommandHandler> _logger;

    public SaveAuthorizationProviderConfigurationCommandHandler(
        IAuthorizationProviderConfigurationService configurationService,
        ILogger<SaveAuthorizationProviderConfigurationCommandHandler> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SaveAuthorizationProviderConfigurationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new AuthorizationProviderConfigurationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid authorization provider configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        request.Configuration.CerbosGrpcEndpoint =
            GrpcEndpointNormalizer.Normalize(request.Configuration.CerbosGrpcEndpoint);

        if (request.Configuration.Provider.Equals("cerbos", StringComparison.OrdinalIgnoreCase))
        {
            var isReachable = await _configurationService.VerifyCerbosEndpointAsync(
                request.Configuration.CerbosGrpcEndpoint,
                cancellationToken);

            if (!isReachable)
            {
                response.Success = false;
                response.Message = "Cerbos gRPC endpoint could not be verified.";
                response.Errors = ["Ensure the endpoint is reachable and serving the gRPC health service."];
                return response;
            }

            if (!string.IsNullOrWhiteSpace(request.Configuration.CerbosAdminEndpoint))
            {
                var isAdminEndpointAllowed = await _configurationService.VerifyCerbosAdminEndpointAsync(
                    request.Configuration.CerbosAdminEndpoint,
                    cancellationToken);

                if (!isAdminEndpointAllowed)
                {
                    response.Success = false;
                    response.Message = "Cerbos Admin API endpoint is not allowed.";
                    response.Errors = ["Use an HTTPS Admin API endpoint without credentials, query, fragment, or local/private network address components."];
                    return response;
                }
            }

            request.Configuration.CerbosEndpointVerified = true;
        }

        await _configurationService.ApplyConfigurationAsync(request.Configuration);

        _logger.LogInformation(
            "Authorization provider configuration saved. Provider: {Provider}, CerbosDetectedFromEnvironment: {Detected}",
            request.Configuration.Provider,
            request.Configuration.CerbosDetectedFromEnvironment);

        response.Success = true;
        response.Message = "Authorization provider configuration saved successfully.";
        return response;
    }
}
