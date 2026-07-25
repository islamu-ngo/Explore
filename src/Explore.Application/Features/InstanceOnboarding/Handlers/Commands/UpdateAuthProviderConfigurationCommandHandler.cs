// ABOUTME: Handles post-onboarding auth provider updates by authorized instance administrators.
// ABOUTME: Prevents admin lockout by ensuring the current admin keeps at least one enabled linked provider.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateAuthProviderConfigurationCommandHandler :
    IRequestHandler<UpdateAuthProviderConfigurationCommand, BaseCommandResponse<Guid>>,
    IRequestHandler<UpdateAuthProviderConfigurationDuringSetupCommand, BaseCommandResponse<Guid>>
{
    private const string KeycloakProvider = "keycloak";
    private const string GoogleProvider = "google";
    private const string AtprotoProvider = "atproto";

    private readonly IAdminContext _adminContext;
    private readonly IUserRepository _userRepository;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IAuthProviderConfigurationService _configurationService;
    private readonly IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier;
    private readonly ISetupSecretProvider _setupSecretProvider;

    public UpdateAuthProviderConfigurationCommandHandler(
        IAdminContext adminContext,
        IUserRepository userRepository,
        IUserExternalLoginRepository userExternalLoginRepository,
        IAuthProviderConfigurationService configurationService,
        IJwtAuthorityRefreshNotifier jwtAuthorityRefreshNotifier,
        ISetupSecretProvider setupSecretProvider)
    {
        _adminContext = adminContext;
        _userRepository = userRepository;
        _userExternalLoginRepository = userExternalLoginRepository;
        _configurationService = configurationService;
        _jwtAuthorityRefreshNotifier = jwtAuthorityRefreshNotifier;
        _setupSecretProvider = setupSecretProvider;
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

        return await ApplyConfigurationAsync(request.Patch, request.UserId, cancellationToken);
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateAuthProviderConfigurationDuringSetupCommand request,
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

        return await ApplyConfigurationAsync(request.Patch, currentAdminUserId: null, cancellationToken);
    }

    private async Task<BaseCommandResponse<Guid>> ApplyConfigurationAsync(
        PatchAuthProviderConfigurationDto configurationPatch,
        Guid? currentAdminUserId,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var currentConfiguration = await _configurationService.ReadConfigurationAsync();
        if (!configurationPatch.HasChanges() || configurationPatch.Configuration.Value is null)
        {
            response.Success = false;
            response.Message = "Authentication provider patch must include a complete configuration group.";
            return response;
        }

        var patch = configurationPatch.Configuration.Value;
        var configuration = new AuthProviderConfigurationDto
        {
            KeycloakEnabled = patch.KeycloakEnabled,
            KeycloakAuthority = patch.KeycloakAuthority,
            KeycloakClientId = patch.KeycloakClientId,
            KeycloakClientSecret = patch.KeycloakClientSecret,
            KeycloakDetectedFromEnvironment = currentConfiguration.KeycloakDetectedFromEnvironment,
            KeycloakClientSecretOwnership = currentConfiguration.KeycloakClientSecretOwnership,
            AtprotoLoginEnabled = patch.AtprotoLoginEnabled,
            AtprotoPublicUrl = patch.AtprotoPublicUrl,
            GoogleSsoEnabled = patch.GoogleSsoEnabled,
            GoogleClientId = patch.GoogleClientId,
            GoogleClientSecret = patch.GoogleClientSecret,
            LockKeycloakEnabled = patch.LockKeycloakEnabled,
            LockAtprotoLoginEnabled = patch.LockAtprotoLoginEnabled,
            LockGoogleSsoEnabled = patch.LockGoogleSsoEnabled
        };
        var validator = new AuthProviderConfigurationDtoValidator(currentConfiguration);
        var validationResult = await validator.ValidateAsync(configuration, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid auth provider configuration.";
            response.Errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return response;
        }

        if (currentAdminUserId.HasValue)
        {
            var currentUser = await _userRepository.GetById(currentAdminUserId.Value);
            if (currentUser == null)
            {
                response.Success = false;
                response.Message = "Current user could not be resolved.";
                return response;
            }

            var currentUserLogins = await _userExternalLoginRepository.GetByUser(currentAdminUserId.Value);
            if (!WouldKeepAtLeastOneProviderEnabledForCurrentAdmin(currentUser.AuthProvider, currentUserLogins, configuration))
            {
                response.Success = false;
                response.Message = "Cannot disable all authentication providers linked to your current admin account.";
                return response;
            }
        }

        await _configurationService.ApplyConfigurationAsync(configuration);
        await _jwtAuthorityRefreshNotifier.ReloadAsync(cancellationToken);

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
