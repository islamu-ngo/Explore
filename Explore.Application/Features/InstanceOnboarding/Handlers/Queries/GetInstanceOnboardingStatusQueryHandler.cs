// ABOUTME: Handles onboarding status queries for startup gating and role-aware onboarding UX.
// ABOUTME: Combines bootstrap completion state with current user instance admin membership.

using System.Text.Json;
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
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISetupSecretProvider _setupSecretProvider;

    public GetInstanceOnboardingStatusQueryHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IUserRoleRepository userRoleRepository,
        ISystemSettingRepository systemSettingRepository,
        ICurrentUserService currentUserService,
        ISetupSecretProvider setupSecretProvider)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _userRoleRepository = userRoleRepository;
        _systemSettingRepository = systemSettingRepository;
        _currentUserService = currentUserService;
        _setupSecretProvider = setupSecretProvider;
    }

    public async Task<InstanceOnboardingStatusDto> Handle(GetInstanceOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var selectedDeploymentMode = !string.IsNullOrWhiteSpace(bootstrap?.SelectedDeploymentMode)
            ? bootstrap!.SelectedDeploymentMode
            : DeserializeString(deploymentModeSetting?.Value, "SingleTenant");

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

        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return response;
        }

        response.IsCurrentUserInstanceAdmin = await _userRoleRepository.IsUserPlatformAdmin(_currentUserService.UserId.Value);
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
}
