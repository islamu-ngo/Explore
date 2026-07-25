// ABOUTME: Handles post-onboarding authorization provider updates by instance administrators.
// ABOUTME: Verifies Cerbos endpoint reachability before allowing the runtime provider to switch to Cerbos.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Utilities;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateAuthorizationProviderConfigurationCommandHandler :
    IRequestHandler<UpdateAuthorizationProviderConfigurationCommand, BaseCommandResponse<Guid>>,
    IRequestHandler<UpdateAuthorizationProviderConfigurationDuringSetupCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IAuthorizationProviderConfigurationService _configurationService;
    private readonly ISetupSecretProvider _setupSecretProvider;

    public UpdateAuthorizationProviderConfigurationCommandHandler(
        IAdminContext adminContext,
        IAuthorizationProviderConfigurationService configurationService,
        ISetupSecretProvider setupSecretProvider)
    {
        _adminContext = adminContext;
        _configurationService = configurationService;
        _setupSecretProvider = setupSecretProvider;
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

        return await ApplyConfigurationAsync(request.Patch, cancellationToken);
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateAuthorizationProviderConfigurationDuringSetupCommand request,
        CancellationToken cancellationToken)
    {
        if (!_setupSecretProvider.IsSetupModeActive || _setupSecretProvider.IsTimedOut)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Setup mode is no longer active."
            };
        }

        return await ApplyConfigurationAsync(request.Patch, cancellationToken);
    }

    private async Task<BaseCommandResponse<Guid>> ApplyConfigurationAsync(
        PatchAuthorizationProviderConfigurationDto configurationPatch,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentConfiguration = await _configurationService.ReadConfigurationAsync();
        if (currentConfiguration.AuthorizationProviderManagedByDeployment)
        {
            response.Success = false;
            response.Message = "Authorization provider configuration is managed by the deployment.";
            response.Errors = ["Change AUTHORIZATION_PROVIDER and the related server-side settings, then restart the deployment."];
            return response;
        }

        if (!configurationPatch.HasChanges() || configurationPatch.Configuration.Value is null)
        {
            response.Success = false;
            response.Message = "Authorization provider patch must include a complete configuration group.";
            return response;
        }

        var patch = configurationPatch.Configuration.Value;
        var configuration = new AuthorizationProviderConfigurationDto
        {
            Provider = patch.Provider,
            CerbosGrpcEndpoint = patch.CerbosGrpcEndpoint,
            CerbosAdminEndpoint = patch.CerbosAdminEndpoint,
            CerbosAdminUsername = patch.CerbosAdminUsername,
            CerbosAdminPassword = patch.CerbosAdminPassword,
            CerbosDetectedFromEnvironment = currentConfiguration.CerbosDetectedFromEnvironment,
            AuthorizationProviderConfigured = currentConfiguration.AuthorizationProviderConfigured,
            AuthorizationProviderManagedByDeployment = currentConfiguration.AuthorizationProviderManagedByDeployment,
            AuthorizationProviderBootstrapStatus = currentConfiguration.AuthorizationProviderBootstrapStatus,
            CerbosPoliciesSynchronized = currentConfiguration.CerbosPoliciesSynchronized,
            AuthorizationProviderBootstrapMessage = currentConfiguration.AuthorizationProviderBootstrapMessage,
            CerbosEndpointOwnership = currentConfiguration.CerbosEndpointOwnership,
            CerbosAdminCredentialsOwnership = currentConfiguration.CerbosAdminCredentialsOwnership
        };

        var validator = new AuthorizationProviderConfigurationDtoValidator();
        var validationResult = await validator.ValidateAsync(configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid authorization provider configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        configuration.CerbosGrpcEndpoint = GrpcEndpointNormalizer.Normalize(configuration.CerbosGrpcEndpoint);

        if (configuration.Provider.Equals("cerbos", StringComparison.OrdinalIgnoreCase))
        {
            var isReachable = await _configurationService.VerifyCerbosEndpointAsync(
                configuration.CerbosGrpcEndpoint,
                cancellationToken);

            if (!isReachable)
            {
                response.Success = false;
                response.Message = "Cerbos gRPC endpoint could not be verified.";
                response.Errors = ["Ensure the endpoint is reachable and serving the gRPC health service."];
                return response;
            }

            if (!string.IsNullOrWhiteSpace(configuration.CerbosAdminEndpoint))
            {
                var isAdminEndpointAllowed = await _configurationService.VerifyCerbosAdminEndpointAsync(
                    configuration.CerbosAdminEndpoint,
                    cancellationToken);

                if (!isAdminEndpointAllowed)
                {
                    response.Success = false;
                    response.Message = "Cerbos Admin API endpoint is not allowed.";
                    response.Errors = ["Use an HTTPS Admin API endpoint without credentials, query, fragment, or local/private network address components."];
                    return response;
                }
            }

            configuration.CerbosEndpointVerified = true;
        }

        await _configurationService.ApplyConfigurationAsync(configuration);

        response.Success = true;
        response.Message = "Authorization provider configuration updated successfully.";
        return response;
    }
}
