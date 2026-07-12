// ABOUTME: Handles post-onboarding authorization provider updates by instance administrators.
// ABOUTME: Verifies Cerbos endpoint reachability before allowing the runtime provider to switch to Cerbos.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Utilities;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateAuthorizationProviderConfigurationCommandHandler : IRequestHandler<UpdateAuthorizationProviderConfigurationCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IAuthorizationProviderConfigurationService _configurationService;

    public UpdateAuthorizationProviderConfigurationCommandHandler(
        IAdminContext adminContext,
        IAuthorizationProviderConfigurationService configurationService)
    {
        _adminContext = adminContext;
        _configurationService = configurationService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateAuthorizationProviderConfigurationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update authorization provider configuration.";
            return response;
        }

        var currentConfiguration = await _configurationService.ReadConfigurationAsync();
        if (currentConfiguration.AuthorizationProviderManagedByDeployment)
        {
            response.Success = false;
            response.Message = "Authorization provider configuration is managed by the deployment.";
            response.Errors = ["Change AUTHORIZATION_PROVIDER and the related server-side settings, then restart the deployment."];
            return response;
        }

        var validator = new AuthorizationProviderConfigurationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid authorization provider configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        request.Configuration.CerbosGrpcEndpoint = GrpcEndpointNormalizer.Normalize(request.Configuration.CerbosGrpcEndpoint);

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

        response.Success = true;
        response.Message = "Authorization provider configuration updated successfully.";
        return response;
    }
}
