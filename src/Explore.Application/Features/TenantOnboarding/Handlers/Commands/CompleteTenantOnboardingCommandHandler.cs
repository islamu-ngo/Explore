// ABOUTME: Handles completion of tenant onboarding by persisting tenant policy choices.
// ABOUTME: Restricts completion to tenant/instance admins and provisions typed branding during completion.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Commands;

public class CompleteTenantOnboardingCommandHandler : IRequestHandler<CompleteTenantOnboardingCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly IAdminContext _adminContext;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver;
    private readonly IMediator _mediator;

    public CompleteTenantOnboardingCommandHandler(
        ITenantContext tenantContext,
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        IAdminContext adminContext,
        ITenantPolicySettingService policySettingService,
        ITenantBrandingSettingsDocumentProvisioningService tenantBrandingProvisioningService,
        IUnitOfWork unitOfWork,
        IHierarchicalSettingsResolver hierarchicalSettingsResolver,
        IMediator mediator)
    {
        _tenantContext = tenantContext;
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _adminContext = adminContext;
        _policySettingService = policySettingService;
        _tenantBrandingProvisioningService = tenantBrandingProvisioningService;
        _unitOfWork = unitOfWork;
        _hierarchicalSettingsResolver = hierarchicalSettingsResolver;
        _mediator = mediator;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CompleteTenantOnboardingCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var tenantId = _tenantContext.TenantId;

        if (!await IsUserAuthorizedAsync(tenantId, cancellationToken))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can complete tenant onboarding.";
            response.FailureCode = FailureCodes.AdminRequired;
            return response;
        }

        // Pre-read for create-or-update decision — BEFORE transaction (fast rejection, no write)
        var existingState = await _tenantOnboardingStateRepository.GetByTenantId(tenantId);

        // Atomic writes: policy settings + typed branding document + onboarding state
        var outcome = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            IReadOnlyList<SettingChangedNotification> notifications =
                await _policySettingService.ApplyTenantSettingsAsync(tenantId, request.UserId, request.Settings, ct);
            await _tenantBrandingProvisioningService.EnsureTenantBrandingDocumentAsync(tenantId, cancellationToken: ct);

            if (existingState == null)
            {
                var created = await _tenantOnboardingStateRepository.Create(new TenantOnboardingState
                {
                    TenantId = tenantId,
                    Tenant = null!,
                    IsCompleted = true,
                    CurrentStep = 4,
                    TotalSteps = 4,
                    CompletedStepsJson = "[\"Identity\",\"Policies\",\"Branding\",\"Review\"]",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    CompletedByUserId = request.UserId
                });
                return (OnboardingStateId: created.Id, Notifications: notifications);
            }

            existingState.IsCompleted = true;
            existingState.CurrentStep = Math.Max(existingState.CurrentStep, 4);
            existingState.TotalSteps = Math.Max(existingState.TotalSteps, 4);
            if (string.IsNullOrWhiteSpace(existingState.CompletedStepsJson))
                existingState.CompletedStepsJson = "[\"Identity\",\"Policies\",\"Branding\",\"Review\"]";
            existingState.CompletedAt = DateTime.UtcNow;
            existingState.CompletedByUserId = request.UserId;
            await _tenantOnboardingStateRepository.Update(existingState);
            return (OnboardingStateId: existingState.Id, Notifications: notifications);
        }, cancellationToken);

        _hierarchicalSettingsResolver.InvalidateCache(Explore.Domain.Settings.SettingScope.Tenant, tenantId);
        foreach (SettingChangedNotification notification in outcome.Notifications)
        {
            await _mediator.Publish(notification, cancellationToken);
        }

        response.Success = true;
        response.Message = "Tenant onboarding completed successfully.";
        response.Id = outcome.OnboardingStateId;
        return response;
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
