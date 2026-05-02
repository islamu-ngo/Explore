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
        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
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

        if (!_currentUserService.IsAuthenticated)
        {
            return response;
        }

        response.IsCurrentUserInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        return response;
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
