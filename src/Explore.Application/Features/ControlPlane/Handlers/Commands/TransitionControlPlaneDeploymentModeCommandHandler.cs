// ABOUTME: Executes audited Control Plane deployment-mode transitions with tenant-count safeguards.
// ABOUTME: Persists operator-selected mode through bootstrap state and invalidates runtime mode cache after commit.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class TransitionControlPlaneDeploymentModeCommandHandler(
    IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
    ITenantRepository tenantRepository,
    ICurrentUserService currentUserService,
    IDeploymentModeProvider deploymentModeProvider,
    ISettingMutationLock mutationLock) : IRequestHandler<TransitionControlPlaneDeploymentModeCommand, BaseCommandResponse<ControlPlaneDeploymentModeTransitionDto>>
{
    public async Task<BaseCommandResponse<ControlPlaneDeploymentModeTransitionDto>> Handle(
        TransitionControlPlaneDeploymentModeCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(DeploymentMode), request.TargetMode))
        {
            return Failure("A valid target deployment mode is required.");
        }

        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Failure("Authenticated operator context is required.");
        }

        var expectedConfirmation = request.TargetMode.ToString();
        if (!string.Equals(request.ConfirmationText?.Trim(), expectedConfirmation, StringComparison.Ordinal))
        {
            return Failure($"Type {expectedConfirmation} to confirm this deployment-mode transition.");
        }

        var transitionedAt = DateTimeOffset.UtcNow;
        var reason = NormalizeReason(request.Reason);
        var selectedMode = request.TargetMode.ToString();

        BaseCommandResponse<ControlPlaneDeploymentModeTransitionDto> response =
            await mutationLock.ExecuteAsync(
                GovernanceSettingKeys.Deployment.Mode,
                async ct =>
                {
                    InstanceBootstrapState? bootstrap = await instanceBootstrapStateRepository.GetCurrent(ct);
                    DeploymentMode currentMode = ResolvePersistedMode(bootstrap);
                    if (currentMode == request.TargetMode)
                    {
                        return Failure($"Deployment mode is already {currentMode}.");
                    }

                    int activeTenantCount = await tenantRepository.GetActiveTenantCountAsync();
                    if (currentMode == DeploymentMode.MultiTenant
                        && request.TargetMode == DeploymentMode.SingleTenant
                        && activeTenantCount > 1)
                    {
                        return Failure(
                            "Cannot switch to single-tenant mode while more than one tenant is active. Suspend or archive extra active tenants first.",
                            FailureCodes.DeploymentModeChangeBlockedByActiveTenants);
                    }

                    if (bootstrap is null)
                    {
                        await instanceBootstrapStateRepository.Create(new InstanceBootstrapState
                        {
                            IsCompleted = true,
                            CreatedAt = transitionedAt.UtcDateTime,
                            CompletedAt = transitionedAt.UtcDateTime,
                            CompletedByUserId = userId.Value,
                            SelectedDeploymentMode = selectedMode
                        });
                    }
                    else
                    {
                        bootstrap.IsCompleted = true;
                        bootstrap.CompletedAt = transitionedAt.UtcDateTime;
                        bootstrap.CompletedByUserId = userId.Value;
                        bootstrap.SelectedDeploymentMode = selectedMode;
                        await instanceBootstrapStateRepository.Update(bootstrap);
                    }

                    return Success(
                        new ControlPlaneDeploymentModeTransitionDto
                        {
                            PreviousMode = currentMode.ToString(),
                            NewMode = selectedMode,
                            ActiveTenantCount = activeTenantCount,
                            OperatorUserId = userId.Value,
                            Reason = reason,
                            TransitionedAtUtc = transitionedAt
                        },
                        $"Deployment mode changed from {currentMode} to {selectedMode}.");
                },
                cancellationToken);

        if (response.Success)
        {
            await deploymentModeProvider.InvalidateCacheAsync();
        }

        return response;
    }

    private static DeploymentMode ResolvePersistedMode(InstanceBootstrapState? bootstrap)
    {
        if (bootstrap?.IsCompleted != true)
        {
            return DeploymentMode.SingleTenant;
        }

        return Enum.TryParse(bootstrap.SelectedDeploymentMode, out DeploymentMode mode)
            ? mode
            : DeploymentMode.MultiTenant;
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var normalized = reason.Trim();
        return normalized.Length <= 1000 ? normalized : normalized[..1000];
    }

    private static BaseCommandResponse<ControlPlaneDeploymentModeTransitionDto> Success(
        ControlPlaneDeploymentModeTransitionDto transition,
        string message) => new()
        {
            Success = true,
            Id = transition,
            Message = message
        };

    private static BaseCommandResponse<ControlPlaneDeploymentModeTransitionDto> Failure(
        string message,
        string? failureCode = null) => new()
        {
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Errors = [message]
        };
}
