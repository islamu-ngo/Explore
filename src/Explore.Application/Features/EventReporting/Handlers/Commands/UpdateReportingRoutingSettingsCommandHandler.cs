// ABOUTME: Handles tenant moderation reporting routing-setting updates through hierarchical settings.
// ABOUTME: Enforces tenant delegation locks and preserves omitted provider secrets.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting.Models;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.EventReporting.Handlers.Commands;

public sealed class UpdateReportingRoutingSettingsCommandHandler(
    ITenantContext tenantContext,
    IAdminContext adminContext,
    IHierarchicalSettingsResolver settingsResolver,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateReportingRoutingSettingsCommand, BaseCommandResponse<Guid>>
{
    private const string LockedFailureCode = "ReportingTenantOverridesLocked";

    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateReportingRoutingSettingsCommand request,
        CancellationToken cancellationToken)
    {
        Guid tenantId = tenantContext.TenantId == Guid.Empty ? request.TenantId : tenantContext.TenantId;
        var response = new BaseCommandResponse<Guid>();

        if (!await IsUserAuthorizedAsync(tenantId, request.UserId, cancellationToken))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can update moderation reporting routing settings.";
            return response;
        }

        TenantDelegationSettingGroup delegation = await settingsResolver.ResolveGroupAsync<TenantDelegationSettingGroup>(
            new SettingContext(tenantId),
            cancellationToken);

        if (delegation.LockReportingProviders)
        {
            return Locked("Tenant moderation reporting provider settings are locked by instance policy.");
        }

        if (delegation.LockTenantOspreyProvider)
        {
            return Locked("Tenant Osprey reporting provider settings are locked by instance policy.");
        }

        if (delegation.LockTenantCoopProvider)
        {
            return Locked("Tenant Coop reporting provider settings are locked by instance policy.");
        }

        List<string> validationErrors = Validate(request.Settings);
        if (validationErrors.Count > 0)
        {
            response.Success = false;
            response.Message = "Moderation reporting routing settings validation failed.";
            response.Errors = validationErrors;
            return response;
        }

        await unitOfWork.ExecuteInTransactionAsync(
            ct => ApplySettingsAsync(tenantId, request.UserId, request.Settings, ct),
            cancellationToken);

        settingsResolver.InvalidateCache(SettingScope.Tenant, tenantId);

        response.Success = true;
        response.Id = tenantId;
        response.Message = "Moderation reporting routing settings updated successfully.";
        return response;
    }

    private async Task<bool> IsUserAuthorizedAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (await adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            return true;
        }

        IReadOnlyList<Guid> adminTenantIds = await adminContext.GetAdminTenantIdsAsync(userId, cancellationToken);
        if (adminTenantIds.Contains(tenantId))
        {
            return true;
        }

        return await adminContext.IsInstanceAdminAsync(userId, cancellationToken);
    }

    private static BaseCommandResponse<Guid> Locked(string message) => new()
    {
        Success = false,
        FailureCode = LockedFailureCode,
        Message = message,
        Errors = ["Instance reporting delegation must be unlocked before tenant reporting provider overrides can be saved."]
    };

    private static List<string> Validate(UpdateReportingRoutingSettingsDto settings)
    {
        var errors = new List<string>();

        if (!IsRoutingMode(settings.OspreyRoutingMode))
        {
            errors.Add("Osprey routing mode must be instance, tenant, or both.");
        }

        if (!IsRoutingMode(settings.CoopRoutingMode))
        {
            errors.Add("Coop routing mode must be instance, tenant, or both.");
        }

        if (!Enum.IsDefined(settings.EvidenceMode))
        {
            errors.Add("Evidence mode is invalid.");
        }

        AddEndpointError(errors, settings.OspreyEndpointUrl, "Osprey endpoint URL");
        AddEndpointError(errors, settings.CoopEndpointUrl, "Coop endpoint URL");

        return errors;
    }

    private static bool IsRoutingMode(string? value) =>
        string.Equals(value, ReportingRoutingMode.Instance, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, ReportingRoutingMode.Tenant, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, ReportingRoutingMode.Both, StringComparison.OrdinalIgnoreCase);

    private static void AddEndpointError(List<string> errors, string? endpointUrl, string label)
    {
        if (endpointUrl is null || endpointUrl.Length == 0)
        {
            return;
        }

        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            errors.Add($"{label} must be an absolute HTTP or HTTPS URL.");
        }
    }

    private async Task ApplySettingsAsync(
        Guid tenantId,
        Guid userId,
        UpdateReportingRoutingSettingsDto settings,
        CancellationToken cancellationToken)
    {
        await SetAsync(GovernanceSettingKeys.Reporting.TenantExternalSyncEnabled, settings.ExternalSyncEnabled, tenantId, userId, cancellationToken);
        await SetAsync(GovernanceSettingKeys.Reporting.EnableTenantOspreyProvider, settings.EnableTenantOspreyProvider, tenantId, userId, cancellationToken);
        await SetAsync(GovernanceSettingKeys.Reporting.EnableTenantCoopProvider, settings.EnableTenantCoopProvider, tenantId, userId, cancellationToken);
        await SetAsync(GovernanceSettingKeys.Reporting.OspreyRoutingMode, NormalizeRoutingMode(settings.OspreyRoutingMode), tenantId, userId, cancellationToken);
        await SetAsync(GovernanceSettingKeys.Reporting.CoopRoutingMode, NormalizeRoutingMode(settings.CoopRoutingMode), tenantId, userId, cancellationToken);
        await SetAsync(GovernanceSettingKeys.Reporting.EvidenceMode, settings.EvidenceMode.ToString(), tenantId, userId, cancellationToken);
        await SetIfProvidedAsync(GovernanceSettingKeys.Reporting.OspreyEndpointUrl, settings.OspreyEndpointUrl, tenantId, userId, cancellationToken, writeWhitespace: true);
        await SetIfProvidedAsync(GovernanceSettingKeys.Reporting.CoopEndpointUrl, settings.CoopEndpointUrl, tenantId, userId, cancellationToken, writeWhitespace: true);
        await SetIfProvidedAsync(InfrastructureSecretSettingKeys.Reporting.OspreyApiKey, settings.OspreyApiKey, tenantId, userId, cancellationToken);
        await SetIfProvidedAsync(InfrastructureSecretSettingKeys.Reporting.OspreyWebhookSecret, settings.OspreyWebhookSecret, tenantId, userId, cancellationToken);
        await SetIfProvidedAsync(InfrastructureSecretSettingKeys.Reporting.CoopApiKey, settings.CoopApiKey, tenantId, userId, cancellationToken);
        await SetIfProvidedAsync(InfrastructureSecretSettingKeys.Reporting.CoopWebhookSecret, settings.CoopWebhookSecret, tenantId, userId, cancellationToken);
    }

    private Task SetAsync<T>(string key, T value, Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        settingsResolver.SetValueAsync(
            key,
            SettingValueSerializer.Serialize(value),
            SettingScope.Tenant,
            tenantId,
            userId,
            cancellationToken);

    private async Task SetIfProvidedAsync(
        string key,
        string? value,
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken,
        bool writeWhitespace = false)
    {
        if (value is null || (!writeWhitespace && string.IsNullOrWhiteSpace(value)))
        {
            return;
        }

        await SetAsync(key, value, tenantId, userId, cancellationToken);
    }

    private static string NormalizeRoutingMode(string value) => value.Trim().ToLowerInvariant() switch
    {
        ReportingRoutingMode.Instance => ReportingRoutingMode.Instance,
        ReportingRoutingMode.Tenant => ReportingRoutingMode.Tenant,
        _ => ReportingRoutingMode.Both
    };
}
