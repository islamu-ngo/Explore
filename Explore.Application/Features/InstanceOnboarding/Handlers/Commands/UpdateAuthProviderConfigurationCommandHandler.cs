// ABOUTME: Handles post-onboarding auth provider updates by authorized instance administrators.
// ABOUTME: Prevents admin lockout by ensuring the current admin keeps at least one enabled linked provider.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateAuthProviderConfigurationCommandHandler : IRequestHandler<UpdateAuthProviderConfigurationCommand, BaseCommandResponse<Guid>>
{
    private const string KeycloakProvider = "keycloak";
    private const string GoogleProvider = "google";
    private const string AtprotoProvider = "atproto";

    private readonly IAdminContext _adminContext;
    private readonly IUserRepository _userRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IAuthProviderConfigurationService _configurationService;

    public UpdateAuthProviderConfigurationCommandHandler(
        IAdminContext adminContext,
        IUserRepository userRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        IAuthProviderConfigurationService configurationService)
    {
        _adminContext = adminContext;
        _userRepository = userRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _configurationService = configurationService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateAuthProviderConfigurationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update auth provider configuration.";
            return response;
        }

        var validator = new AuthProviderConfigurationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid auth provider configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        var currentUser = await _userRepository.GetById(request.UserId);
        if (currentUser == null)
        {
            response.Success = false;
            response.Message = "Current user could not be resolved.";
            return response;
        }

        var currentUserLogins = await _userExternalLoginRepository.GetByUser(request.UserId);
        if (!WouldKeepAtLeastOneProviderEnabledForCurrentAdmin(currentUser.AuthProvider, currentUserLogins, request.Configuration))
        {
            response.Success = false;
            response.Message = "Cannot disable all authentication providers linked to your current admin account.";
            return response;
        }

        await _configurationService.ApplyConfigurationAsync(request.Configuration);

        response.Success = true;
        response.Message = "Authentication provider configuration updated successfully.";
        return response;
    }

    private static bool WouldKeepAtLeastOneProviderEnabledForCurrentAdmin(
        string? userPrimaryProvider,
        List<Domain.UserExternalLogin> userExternalLogins,
        DTOs.Onboarding.AuthProviderConfigurationDto configuration)
    {
        var enabledProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (configuration.KeycloakEnabled)
        {
            enabledProviders.Add(KeycloakProvider);
        }

        if (configuration.GoogleSsoEnabled)
        {
            enabledProviders.Add(GoogleProvider);
        }

        if (configuration.AtprotoLoginEnabled)
        {
            enabledProviders.Add(AtprotoProvider);
        }

        var linkedProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(userPrimaryProvider))
        {
            linkedProviders.Add(userPrimaryProvider.Trim());
        }

        foreach (var login in userExternalLogins)
        {
            if (!string.IsNullOrWhiteSpace(login.Provider))
            {
                linkedProviders.Add(login.Provider.Trim());
            }
        }

        if (linkedProviders.Count == 0)
        {
            return true;
        }

        return linkedProviders.Any(enabledProviders.Contains);
    }
}
