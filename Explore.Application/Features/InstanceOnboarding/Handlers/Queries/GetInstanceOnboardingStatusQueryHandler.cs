// ABOUTME: Handles onboarding status queries for startup gating and role-aware onboarding UX.
// ABOUTME: Combines bootstrap completion state with current user instance admin membership.

using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public class GetInstanceOnboardingStatusQueryHandler : IRequestHandler<GetInstanceOnboardingStatusQuery, InstanceOnboardingStatusDto>
{
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly IAdminContext _adminContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISetupSecretProvider _setupSecretProvider;
    private readonly IDeploymentModeProvider _deploymentModeProvider;

    public GetInstanceOnboardingStatusQueryHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IAdminContext adminContext,
        ISystemSettingRepository systemSettingRepository,
        ICurrentUserService currentUserService,
        ISetupSecretProvider setupSecretProvider,
        IDeploymentModeProvider deploymentModeProvider)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _adminContext = adminContext;
        _systemSettingRepository = systemSettingRepository;
        _currentUserService = currentUserService;
        _setupSecretProvider = setupSecretProvider;
        _deploymentModeProvider = deploymentModeProvider;
    }

    public async Task<InstanceOnboardingStatusDto> Handle(GetInstanceOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent(cancellationToken);
        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode);
        var selectedDeploymentMode = await ResolveDeploymentModeAsync(bootstrap?.SelectedDeploymentMode, bootstrap?.IsCompleted == true, deploymentModeSetting?.Value, cancellationToken);

        var response = new InstanceOnboardingStatusDto
        {
            IsCompleted = bootstrap?.IsCompleted == true,
            IsAuthenticated = _currentUserService.IsAuthenticated,
            IsCurrentUserInstanceAdmin = false,
            SelectedDeploymentMode = selectedDeploymentMode,
            IsSetupModeActive = _setupSecretProvider.IsSetupModeActive,
            SetupSecretFromEnvironment = _setupSecretProvider.IsFromEnvironmentVariable,
            SetupTimedOut = _setupSecretProvider.IsTimedOut,
            InstanceStartedAt = _setupSecretProvider.InstanceStartedAt
        };
        ApplySetupSecretStatus(response);

        if (!_currentUserService.IsAuthenticated)
        {
            return response;
        }

        response.IsCurrentUserInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        return response;
    }

    private static void ApplySetupSecretStatus(InstanceOnboardingStatusDto response)
    {
        if (response.IsCompleted)
        {
            response.SetupSecretState = "Locked";
            response.SetupSecretGuidance = "Setup is complete. The setup secret is locked and can no longer be used.";
            return;
        }

        if (response.SetupTimedOut)
        {
            response.SetupSecretState = "Expired";
            response.SetupSecretGuidance = response.SetupSecretFromEnvironment
                ? "The configured SETUP_SECRET is still authoritative, but this setup session has timed out. Restart the application to reopen setup mode."
                : "The generated setup secret has expired. Restart the application and use the newly logged setup secret.";
            return;
        }

        if (!response.IsSetupModeActive)
        {
            response.SetupSecretState = "Unavailable";
            response.SetupSecretGuidance = "Setup mode is not active. If onboarding is incomplete, restart the application and check startup logs.";
            return;
        }

        if (response.SetupSecretFromEnvironment)
        {
            response.SetupSecretState = "Environment";
            response.SetupSecretGuidance = "Use the SETUP_SECRET environment variable configured for this deployment.";
            return;
        }

        response.SetupSecretState = "Generated";
        response.SetupSecretGuidance = "Use the generated setup secret from the API startup logs. Generated secrets expire 60 minutes after startup.";
    }

    private static string DeserializeString(string? rawValue, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }

    private async Task<string> ResolveDeploymentModeAsync(
        string? bootstrapMode,
        bool isCompleted,
        string? deploymentModeSettingValue,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(bootstrapMode))
        {
            return bootstrapMode;
        }

        if (!isCompleted)
        {
            return (await _deploymentModeProvider.GetConfiguredOnboardingModeAsync(cancellationToken)).ToString();
        }

        var currentMode = (await _deploymentModeProvider.GetCurrentModeAsync(cancellationToken)).ToString();
        return DeserializeString(deploymentModeSettingValue, currentMode);
    }
}
