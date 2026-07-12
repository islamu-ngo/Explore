// ABOUTME: Handles saving auth provider configuration during instance setup (setup-token-protected).
// ABOUTME: Validates configuration, checks credential connectivity, and persists via service layer.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class SaveAuthProviderConfigurationCommandHandler : IRequestHandler<SaveAuthProviderConfigurationCommand, BaseCommandResponse<Guid>>
{
    private readonly IAuthProviderConfigurationService _configurationService;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly ILogger<SaveAuthProviderConfigurationCommandHandler> _logger;

    public SaveAuthProviderConfigurationCommandHandler(
        IAuthProviderConfigurationService configurationService,
        IJwtAuthorityRefreshNotifier jwtAuthorityRefreshNotifier,
        ILogger<SaveAuthProviderConfigurationCommandHandler> logger)
    {
        _configurationService = configurationService;
        _jwtAuthorityRefreshNotifier = jwtAuthorityRefreshNotifier;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SaveAuthProviderConfigurationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentConfiguration = await _configurationService.ReadConfigurationAsync();
        var validator = new AuthProviderConfigurationDtoValidator(currentConfiguration);
        var validationResult = await validator.ValidateAsync(request.Configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid auth provider configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        await _configurationService.ApplyConfigurationAsync(request.Configuration);
        await _jwtAuthorityRefreshNotifier.ReloadAsync(cancellationToken);

        _logger.LogInformation(
            "Auth provider configuration saved. Keycloak: {KeycloakEnabled}, ATProto: {AtprotoEnabled}, Google: {GoogleEnabled}",
            request.Configuration.KeycloakEnabled,
            request.Configuration.AtprotoLoginEnabled,
            request.Configuration.GoogleSsoEnabled);

        response.Success = true;
        response.Message = "Authentication provider configuration saved successfully.";
        return response;
    }
}
