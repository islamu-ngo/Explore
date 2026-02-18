// ABOUTME: Handles tenant onboarding step progress persistence with analytics tracking.

using System.Linq;
using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Commands;

public class SaveTenantOnboardingStepCommandHandler : IRequestHandler<SaveTenantOnboardingStepCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly IAnalyticsProvider _analyticsProvider;
    private readonly IAdminContext _adminContext;

    public SaveTenantOnboardingStepCommandHandler(
        ITenantContext tenantContext,
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        IAnalyticsProvider analyticsProvider,
        IAdminContext adminContext)
    {
        _tenantContext = tenantContext;
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _analyticsProvider = analyticsProvider;
        _adminContext = adminContext;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(SaveTenantOnboardingStepCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var tenantId = _tenantContext.TenantId;

        if (!await IsUserAuthorizedAsync(tenantId, cancellationToken))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can update tenant onboarding progress.";
            return response;
        }

        var onboardingState = await _tenantOnboardingStateRepository.GetByTenantId(tenantId);
        if (onboardingState == null)
        {
            onboardingState = await _tenantOnboardingStateRepository.Create(new TenantOnboardingState
            {
                TenantId = tenantId,
                Tenant = null!,
                IsCompleted = false,
                CurrentStep = NormalizeStep(request.CurrentStep, request.TotalSteps),
                TotalSteps = NormalizeTotalSteps(request.TotalSteps),
                CompletedStepsJson = SerializeCompletedSteps(request.CompletedSteps),
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            onboardingState.CurrentStep = NormalizeStep(request.CurrentStep, request.TotalSteps);
            onboardingState.TotalSteps = NormalizeTotalSteps(request.TotalSteps);
            onboardingState.CompletedStepsJson = SerializeCompletedSteps(request.CompletedSteps);
            await _tenantOnboardingStateRepository.Update(onboardingState);
        }

        response.Success = true;
        response.Message = "Tenant onboarding progress saved.";
        response.Id = onboardingState.Id;

        await TrackStepAsync(request.UserId, onboardingState.CurrentStep, onboardingState.TotalSteps, request.CompletedSteps, cancellationToken);

        return response;
    }

    private async Task TrackStepAsync(
        Guid userId,
        int currentStep,
        int totalSteps,
        string[] completedSteps,
        CancellationToken cancellationToken)
    {
        var stepName = completedSteps.LastOrDefault() ?? string.Empty;
        var properties = new Dictionary<string, object>
        {
            ["tenant_id"] = _tenantContext.TenantId,
            ["step_index"] = currentStep,
            ["step_name"] = stepName,
            ["total_steps"] = totalSteps,
            ["completed_steps"] = completedSteps
        };

        await _analyticsProvider.TrackAsync(userId.ToString(), "onboarding.step_completed", properties, cancellationToken);
    }

    private static string SerializeCompletedSteps(string[] completedSteps)
    {
        if (completedSteps.Length == 0)
        {
            return "[]";
        }

        return JsonSerializer.Serialize(completedSteps);
    }

    private static int NormalizeTotalSteps(int totalSteps)
    {
        return totalSteps < 0 ? 0 : totalSteps;
    }

    private static int NormalizeStep(int currentStep, int totalSteps)
    {
        var normalizedTotal = NormalizeTotalSteps(totalSteps);
        if (normalizedTotal == 0)
        {
            return Math.Max(0, currentStep);
        }

        var clamped = Math.Clamp(currentStep, 0, normalizedTotal);
        return clamped;
    }

    private async Task<bool> IsUserAuthorizedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            return true;
        }

        return await _adminContext.IsInstanceAdminAsync(cancellationToken);
    }
}
