// ABOUTME: Handles runtime updates to tenant policy settings after onboarding.
// ABOUTME: Enforces tenant administrator (Owner/Admin) or instance administrator authorization before applying overrides.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Exceptions;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Responses;
using FluentValidation.Results;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Commands;

public class UpdateTenantPolicySettingsCommandHandler : IRequestHandler<UpdateTenantPolicySettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly IAdminContext _adminContext;
    private readonly ITenantPolicySettingService _policySettingService;

    public UpdateTenantPolicySettingsCommandHandler(
        ITenantContext tenantContext,
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        IAdminContext adminContext,
        ITenantPolicySettingService policySettingService)
    {
        _tenantContext = tenantContext;
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _adminContext = adminContext;
        _policySettingService = policySettingService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantPolicySettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var tenantId = _tenantContext.TenantId;

        if (!await IsUserAuthorizedAsync(tenantId, cancellationToken))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can update tenant settings.";
            return response;
        }

        await EnsureLockedSettingsAreNotModifiedAsync(tenantId, request.Settings);

        await _policySettingService.ApplyTenantSettingsAsync(tenantId, request.UserId, request.Settings);

        var onboardingState = await _tenantOnboardingStateRepository.GetByTenantId(tenantId);
        response.Id = onboardingState?.Id ?? Guid.Empty;
        response.Success = true;
        response.Message = "Tenant settings updated successfully.";
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

    private async Task EnsureLockedSettingsAreNotModifiedAsync(Guid tenantId, UpdateTenantPolicyRequest requestSettings)
    {
        var effectiveSettings = await _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId);
        var failures = new List<ValidationFailure>();

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.RequireOrganizationVerification),
            effectiveSettings.CanTenantOmitVerification,
            effectiveSettings.RequireOrganizationVerification,
            requestSettings.RequireOrganizationVerification,
            "Organization verification requirement is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.PreferredHomePage),
            effectiveSettings.CanOverrideHomePagePreference,
            effectiveSettings.PreferredHomePage,
            requestSettings.PreferredHomePage,
            "Preferred home page is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.Subdomain),
            effectiveSettings.CanOverrideSubdomain,
            effectiveSettings.Subdomain,
            requestSettings.Subdomain,
            "Tenant subdomain is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.CustomDomain),
            effectiveSettings.CanOverrideCustomDomain,
            effectiveSettings.CustomDomain,
            requestSettings.CustomDomain,
            "Tenant custom domain is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.BrandDisplayName),
            effectiveSettings.CanOverrideBrandDisplayName,
            effectiveSettings.BrandDisplayName,
            requestSettings.BrandDisplayName,
            "Tenant brand display name is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.BrandLogoUrl),
            effectiveSettings.CanOverrideBrandLogoUrl,
            effectiveSettings.BrandLogoUrl,
            requestSettings.BrandLogoUrl,
            "Tenant brand logo URL is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.BrandFaviconUrl),
            effectiveSettings.CanOverrideBrandFaviconUrl,
            effectiveSettings.BrandFaviconUrl,
            requestSettings.BrandFaviconUrl,
            "Tenant brand favicon URL is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.BrandCustomCssUrl),
            effectiveSettings.CanOverrideBrandCustomCssUrl,
            effectiveSettings.BrandCustomCssUrl,
            requestSettings.BrandCustomCssUrl,
            "Tenant brand custom CSS URL is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.EventCardClickOpensDetailPage),
            effectiveSettings.CanOverrideEventCardClickBehavior,
            effectiveSettings.EventCardClickOpensDetailPage,
            requestSettings.EventCardClickOpensDetailPage,
            "Event card click behavior is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.RenderPolicyPreset),
            effectiveSettings.CanOverrideRenderPolicy,
            effectiveSettings.RenderPolicyPreset,
            requestSettings.RenderPolicyPreset,
            "Render policy preset is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.GlobalRenderMode),
            effectiveSettings.CanOverrideRenderPolicy,
            effectiveSettings.GlobalRenderMode,
            requestSettings.GlobalRenderMode,
            "Global render mode is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.PublicSeoRenderMode),
            effectiveSettings.CanOverridePublicSeoRenderPolicy,
            effectiveSettings.PublicSeoRenderMode,
            requestSettings.PublicSeoRenderMode,
            "Public/SEO render policy is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.OperationalRenderMode),
            effectiveSettings.CanOverrideOperationalRenderPolicy,
            effectiveSettings.OperationalRenderMode,
            requestSettings.OperationalRenderMode,
            "Operational render policy is locked by instance policy.");

        AddLockedValueChangeFailure(
            failures,
            nameof(requestSettings.AdminRenderMode),
            effectiveSettings.CanOverrideAdminRenderPolicy,
            effectiveSettings.AdminRenderMode,
            requestSettings.AdminRenderMode,
            "Admin render policy is locked by instance policy.");

        if (failures.Count > 0)
        {
            throw new ValidationException(new ValidationResult(failures));
        }
    }

    private static void AddLockedValueChangeFailure(
        List<ValidationFailure> failures,
        string propertyName,
        bool canOverride,
        object? effectiveValue,
        object? requestedValue,
        string message)
    {
        if (canOverride)
        {
            return;
        }

        if (ValuesEqual(effectiveValue, requestedValue))
        {
            return;
        }

        failures.Add(new ValidationFailure(propertyName, message));
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (left is string leftString && right is string rightString)
        {
            return string.Equals(leftString.Trim(), rightString.Trim(), StringComparison.Ordinal);
        }

        return Equals(left, right);
    }
}
