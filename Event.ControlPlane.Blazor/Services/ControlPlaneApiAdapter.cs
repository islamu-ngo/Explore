// ABOUTME: Adapter from generated Event API client DTOs into control-plane UI service contracts.
// ABOUTME: Centralizes HAL mapping and API error translation so Razor components stay transport-agnostic.

using System.Collections;
using System.Globalization;
using System.Reflection;
using Event.ControlPlane.Blazor.Clients;
using Event.ControlPlane.Client.Contracts;
using Event.ControlPlane.Client.Services;
using Microsoft.Extensions.Logging;

namespace Event.ControlPlane.Blazor.Services;

public sealed class ControlPlaneApiAdapter(
    IEventApiClient apiClient,
    ILogger<ControlPlaneApiAdapter> logger)
    : IControlPlaneOverviewService,
      IControlPlaneTenantService,
      IControlPlaneDomainService,
      IControlPlaneOperationsService,
      IControlPlanePlanService,
      IControlPlaneTenantConfigurationService
{
    public async Task<ControlPlaneResult<ControlPlaneOverview>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var overview = await apiClient.GetControlPlaneOverviewAsync(cancellationToken: cancellationToken);

            return ControlPlaneResult.Success(new ControlPlaneOverview(
                overview.DeploymentMode ?? "unknown",
                overview.Version,
                overview.PublicOrigin,
                overview.AdminOrigin,
                MapOverviewStatusCards(overview),
                MapWarnings(overview.Warnings),
                MapLinks(overview._links)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return ApiFailure<ControlPlaneOverview>(ex);
        }
        catch (Exception ex)
        {
            return UnexpectedFailure<ControlPlaneOverview>(ex);
        }
    }

    public async Task<ControlPlaneResult<ControlPlaneTenantList>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tenants = await apiClient.GetControlPlaneTenantsAsync(cancellationToken: cancellationToken);
            var items = tenants._embedded?.Items?.Select(MapTenant).ToArray() ?? [];

            return ControlPlaneResult.Success(new ControlPlaneTenantList(
                items,
                tenants.TotalCount ?? items.Length,
                MapLinks(tenants._links)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return ApiFailure<ControlPlaneTenantList>(ex);
        }
        catch (Exception ex)
        {
            return UnexpectedFailure<ControlPlaneTenantList>(ex);
        }
    }

    public async Task<ControlPlaneResult<ControlPlaneDomainList>> GetDomainsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var domains = await apiClient.GetControlPlaneDomainsAsync(cancellationToken: cancellationToken);
            var records = domains.DnsRecords?.Select(MapDomain).ToArray() ?? [];

            return ControlPlaneResult.Success(new ControlPlaneDomainList(records, MapLinks(domains._links)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return ApiFailure<ControlPlaneDomainList>(ex);
        }
        catch (Exception ex)
        {
            return UnexpectedFailure<ControlPlaneDomainList>(ex);
        }
    }

    public async Task<ControlPlaneResult<ControlPlaneOperations>> GetOperationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var operations = await apiClient.GetControlPlaneOperationsAsync(cancellationToken: cancellationToken);

            return ControlPlaneResult.Success(new ControlPlaneOperations(
                operations.GeneratedAtUtc ?? DateTimeOffset.MinValue,
                operations.Statuses?.Select(MapOperationStatus).ToArray() ?? [],
                MapWarnings(operations.Warnings),
                MapLinks(operations._links)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return ApiFailure<ControlPlaneOperations>(ex);
        }
        catch (Exception ex)
        {
            return UnexpectedFailure<ControlPlaneOperations>(ex);
        }
    }

    public async Task<ControlPlaneResult<ControlPlaneDeploymentModeRunbook>> GetDeploymentModeRunbookAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var runbook = await apiClient.GetControlPlaneDeploymentModeRunbookAsync(cancellationToken: cancellationToken);

            return ControlPlaneResult.Success(new ControlPlaneDeploymentModeRunbook(
                runbook.CurrentMode ?? string.Empty,
                runbook.ActiveTenantCount ?? 0,
                runbook.GeneratedAtUtc ?? DateTimeOffset.MinValue,
                runbook.TargetOptions?.Select(MapDeploymentModeTargetOption).ToArray() ?? [],
                runbook.Steps?.Select(MapDeploymentModeRunbookStep).ToArray() ?? [],
                MapLinks(runbook._links)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return ApiFailure<ControlPlaneDeploymentModeRunbook>(ex);
        }
        catch (Exception ex)
        {
            return UnexpectedFailure<ControlPlaneDeploymentModeRunbook>(ex);
        }
    }

    public Task<ControlPlaneResult<ControlPlaneTenantPlanList>> GetPlansAsync(
        CancellationToken cancellationToken = default) =>
        SendTenantPlanQueryAsync(
            token => apiClient.GetControlPlaneTenantPlansAsync(cancellationToken: token),
            MapTenantPlanList,
            cancellationToken);

    public Task<ControlPlaneResult<ControlPlaneTenantPlanDetail>> GetPlanAsync(
        string key,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanQueryAsync(
            token => apiClient.GetControlPlaneTenantPlanByKeyAsync(key, cancellationToken: token),
            MapTenantPlanDetail,
            cancellationToken);

    public Task<ControlPlaneCommandResult> CreatePlanDraftAsync(
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.CreateControlPlaneTenantPlanDraftAsync(
                ToTenantPlanDraft(draft),
                cancellationToken: token),
            "Tenant plan draft created.",
            "The tenant plan draft could not be created.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> CreatePlanVersionDraftAsync(
        string key,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.CreateControlPlaneTenantPlanVersionDraftAsync(
                key,
                ToTenantPlanDraft(draft),
                cancellationToken: token),
            "Tenant plan version draft created.",
            "The tenant plan version draft could not be created.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> UpdatePlanVersionDraftAsync(
        Guid versionId,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.UpdateControlPlaneTenantPlanVersionDraftAsync(
                versionId,
                ToTenantPlanDraft(draft),
                cancellationToken: token),
            "Tenant plan version draft updated.",
            "The tenant plan version draft could not be updated.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> PublishPlanVersionAsync(
        Guid versionId,
        ControlPlaneTenantPlanExistingAssignmentPolicy existingTenantPolicy,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.PublishControlPlaneTenantPlanVersionAsync(
                versionId,
                new PublishTenantPlanVersionRequest { ExistingTenantPolicy = (int)existingTenantPolicy },
                cancellationToken: token),
            "Tenant plan version published.",
            "The tenant plan version could not be published.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> ArchivePlanVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.ArchiveControlPlaneTenantPlanVersionAsync(
                versionId,
                cancellationToken: token),
            "Tenant plan version archived.",
            "The tenant plan version could not be archived.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> ClonePlanAsync(
        Guid sourceVersionId,
        string key,
        string name,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.CloneControlPlaneTenantPlanAsync(
                sourceVersionId,
                new CloneTenantPlanRequest { Key = key, Name = name },
                cancellationToken: token),
            "Tenant plan cloned.",
            "The tenant plan could not be cloned.",
            cancellationToken);

    public Task<ControlPlaneResult<ControlPlaneTenantPlanValidationResult>> ValidatePlanDraftAsync(
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanQueryAsync(
            token => apiClient.ValidateControlPlaneTenantPlanDraftAsync(
                ToTenantPlanDraft(draft),
                cancellationToken: token),
            MapTenantPlanValidationResult,
            cancellationToken);

    public Task<ControlPlaneResult<ControlPlaneTenantPlanDiffResult>> PreviewPlanDiffAsync(
        ControlPlaneTenantPlanEffectiveConfiguration current,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanQueryAsync(
            token => apiClient.PreviewControlPlaneTenantPlanDiffAsync(
                new PreviewTenantPlanDiffRequest
                {
                    Current = ToTenantPlanEffectiveConfiguration(current),
                    Draft = ToTenantPlanDraft(draft)
                },
                cancellationToken: token),
            MapTenantPlanDiffResult,
            cancellationToken);

    public Task<ControlPlaneResult<ControlPlaneTenantPlanAssignment>> GetTenantPlanAssignmentAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanQueryAsync(
            token => apiClient.GetControlPlaneTenantPlanAssignmentAsync(tenantId, cancellationToken: token),
            MapTenantPlanAssignment,
            cancellationToken);

    public Task<ControlPlaneResult<ControlPlaneTenantEffectiveConfiguration>> GetEffectiveConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanQueryAsync(
            token => apiClient.GetControlPlaneTenantEffectiveConfigurationAsync(tenantId, cancellationToken: token),
            MapTenantEffectiveConfiguration,
            cancellationToken);

    public Task<ControlPlaneCommandResult> SetSettingAsync(
        Guid tenantId,
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.SetControlPlaneTenantSettingAsync(
                tenantId,
                key,
                new SetControlPlaneTenantSettingRequest { Value = value },
                cancellationToken: token),
            "Tenant setting updated.",
            "The tenant setting could not be updated.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> LockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.LockControlPlaneTenantSettingAsync(tenantId, key, cancellationToken: token),
            "Tenant setting locked.",
            "The tenant setting could not be locked.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> UnlockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.UnlockControlPlaneTenantSettingAsync(tenantId, key, cancellationToken: token),
            "Tenant setting unlocked.",
            "The tenant setting could not be unlocked.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> SwitchTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid tenantPlanVersionId,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.SwitchControlPlaneTenantPlanAssignmentAsync(
                tenantId,
                new SwitchTenantPlanAssignmentRequest { TenantPlanVersionId = tenantPlanVersionId },
                cancellationToken: token),
            "Tenant plan assignment updated.",
            "The tenant plan assignment could not be updated.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> ApplyTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.ApplyControlPlaneTenantPlanAssignmentAsync(
                tenantId,
                assignmentId,
                cancellationToken: token),
            "Tenant plan assignment applied.",
            "The tenant plan assignment could not be applied.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> RollbackTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        SendTenantPlanCommandAsync(
            token => apiClient.RollbackControlPlaneTenantPlanAssignmentAsync(
                tenantId,
                assignmentId,
                cancellationToken: token),
            "Tenant plan assignment rolled back.",
            "The tenant plan assignment could not be rolled back.",
            cancellationToken);

    public Task<ControlPlaneCommandResult> TransitionDeploymentModeAsync(
        string targetMode,
        string confirmationText,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        SendDeploymentModeTransitionCommandAsync(
            token => apiClient.TransitionControlPlaneDeploymentModeAsync(
                body: new ControlPlaneDeploymentModeTransitionRequestDto
                {
                    TargetMode = targetMode,
                    ConfirmationText = confirmationText,
                    Reason = reason
                },
                cancellationToken: token),
            cancellationToken);

    public Task<ControlPlaneCommandResult> ActivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        SendTenantLifecycleCommandAsync(
            token => apiClient.ActivateControlPlaneTenantAsync(
                tenantId,
                body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
                cancellationToken: token),
            cancellationToken);

    public Task<ControlPlaneCommandResult> SuspendTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        SendTenantLifecycleCommandAsync(
            token => apiClient.SuspendControlPlaneTenantAsync(
                tenantId,
                body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
                cancellationToken: token),
            cancellationToken);

    public Task<ControlPlaneCommandResult> ArchiveTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        SendTenantLifecycleCommandAsync(
            token => apiClient.ArchiveControlPlaneTenantAsync(
                tenantId,
                body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
                cancellationToken: token),
            cancellationToken);

    public Task<ControlPlaneCommandResult> ReactivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        SendTenantLifecycleCommandAsync(
            token => apiClient.ReactivateControlPlaneTenantAsync(
                tenantId,
                body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
                cancellationToken: token),
            cancellationToken);

    public Task<ControlPlaneCommandResult> ScheduleTenantPurgeAsync(
        Guid tenantId,
        string reason,
        string confirmationText,
        CancellationToken cancellationToken = default) =>
        SendTenantLifecycleCommandAsync(
            token => apiClient.ScheduleControlPlaneTenantPurgeAsync(
                tenantId,
                body: new ControlPlaneTenantLifecycleTransitionRequestDto
                {
                    Reason = reason,
                    ConfirmationText = confirmationText
                },
                cancellationToken: token),
            cancellationToken);

    private static ControlPlaneTenantSummary MapTenant(HalResourceOfControlPlaneTenantListItemDto tenant)
    {
        var slug = tenant.Slug ?? string.Empty;
        var name = tenant.FullName ?? slug;

        return new ControlPlaneTenantSummary(
            tenant.Id ?? Guid.Empty,
            string.IsNullOrWhiteSpace(name) ? "Unnamed tenant" : name,
            slug,
            tenant.StatusName ?? tenant.StatusCode ?? "Unknown",
            null,
            null,
            MapLinks(tenant._links));
    }

    private static ControlPlaneTenantPlanList MapTenantPlanList(
        HalCollectionResourceOfControlPlaneTenantPlanListItemDto plans)
    {
        var items = plans._embedded?.Items?.Select(MapTenantPlanSummary).ToArray() ?? [];

        return new ControlPlaneTenantPlanList(
            items,
            plans.TotalCount ?? items.Length,
            MapLinks(plans._links));
    }

    private static ControlPlaneTenantPlanSummary MapTenantPlanSummary(HalResourceOfControlPlaneTenantPlanListItemDto plan)
    {
        var key = string.IsNullOrWhiteSpace(plan.Key) ? "unknown" : plan.Key;

        return new ControlPlaneTenantPlanSummary(
            plan.Id ?? Guid.Empty,
            key,
            string.IsNullOrWhiteSpace(plan.DisplayName) ? key : plan.DisplayName,
            plan.Description,
            plan.LatestVersionNumber ?? 0,
            plan.PublishedVersionNumber,
            (decimal)(plan.PriceAmount ?? 0),
            plan.CurrencyCode ?? string.Empty,
            plan.BillingPeriod ?? string.Empty,
            plan.IsActiveForProvisioning == true,
            MapLinks(plan._links));
    }

    private static ControlPlaneTenantPlanDetail MapTenantPlanDetail(HalResourceOfControlPlaneTenantPlanDetailDto plan)
    {
        var key = string.IsNullOrWhiteSpace(plan.Key) ? "unknown" : plan.Key;

        return new ControlPlaneTenantPlanDetail(
            plan.Id ?? Guid.Empty,
            key,
            string.IsNullOrWhiteSpace(plan.DisplayName) ? key : plan.DisplayName,
            plan.Description,
            plan.Versions?.Select(MapTenantPlanVersion).ToArray() ?? [],
            MapLinks(plan._links));
    }

    private static ControlPlaneTenantPlanVersion MapTenantPlanVersion(ControlPlaneTenantPlanVersionDto version) =>
        new(
            version.Id ?? Guid.Empty,
            version.VersionNumber ?? 0,
            version.StatusId ?? 0,
            version.StatusCode ?? "UNKNOWN",
            (decimal)(version.PriceAmount ?? 0),
            version.CurrencyCode ?? string.Empty,
            version.BillingPeriod ?? string.Empty,
            version.IsActiveForProvisioning == true,
            version.Settings?.Select(MapTenantPlanSetting).ToArray() ?? [],
            version.Quotas?.Select(MapTenantPlanQuota).ToArray() ?? []);

    private static ControlPlaneTenantPlanSetting MapTenantPlanSetting(ControlPlaneTenantPlanSettingDto setting) =>
        new(
            setting.Key ?? "unknown",
            setting.JsonValue ?? string.Empty,
            setting.IsLocked == true);

    private static ControlPlaneTenantPlanQuota MapTenantPlanQuota(ControlPlaneTenantPlanQuotaDto quota) =>
        new(
            quota.Key ?? "unknown",
            quota.Limit ?? 0);

    private static ControlPlaneTenantPlanValidationResult MapTenantPlanValidationResult(
        TenantPlanValidationResult result) =>
        new(result.Errors?.Select(MapTenantPlanValidationError).ToArray() ?? []);

    private static ControlPlaneTenantPlanValidationError MapTenantPlanValidationError(
        TenantPlanValidationError error) =>
        new(
            error.Code ?? "unknown",
            error.Target ?? string.Empty,
            error.Message ?? string.Empty);

    private static ControlPlaneTenantPlanDiffResult MapTenantPlanDiffResult(TenantPlanDiffResult result) =>
        new(result.SettingChanges?.Select(MapTenantPlanSettingChange).ToArray() ?? []);

    private static ControlPlaneTenantPlanSettingChange MapTenantPlanSettingChange(TenantPlanSettingChange change) =>
        new(
            change.Key ?? "unknown",
            (ControlPlaneTenantPlanChangeType)change.ChangeType,
            change.BeforeValue,
            change.AfterValue,
            change.LockChanged);

    private static ControlPlaneTenantPlanAssignment MapTenantPlanAssignment(ControlPlaneTenantPlanAssignmentDto assignment) =>
        new(
            assignment.Id ?? Guid.Empty,
            assignment.TenantId ?? Guid.Empty,
            assignment.PlanId ?? Guid.Empty,
            assignment.PlanKey ?? "unknown",
            assignment.PlanVersionId ?? Guid.Empty,
            assignment.VersionNumber ?? 0,
            assignment.StatusId ?? 0,
            assignment.StatusCode ?? "UNKNOWN",
            assignment.AssignedAt ?? DateTimeOffset.MinValue,
            assignment.AssignedByUserId);

    private static ControlPlaneTenantEffectiveConfiguration MapTenantEffectiveConfiguration(
        HalResourceOfControlPlaneTenantEffectiveConfigurationDto configuration) =>
        new(
            configuration.TenantId ?? Guid.Empty,
            configuration.PlanAssignment is null ? null : MapTenantPlanAssignment(configuration.PlanAssignment),
            configuration.Settings?.Select(MapTenantEffectiveSetting).ToArray() ?? [],
            configuration.Quotas?.Select(MapTenantQuotaUsage).ToArray() ?? [],
            MapLinks(configuration._links));

    private static ControlPlaneTenantEffectiveSetting MapTenantEffectiveSetting(
        ControlPlaneTenantEffectiveSettingDto setting) =>
        new(
            setting.Key ?? "unknown",
            setting.Category ?? string.Empty,
            setting.Value ?? string.Empty,
            setting.SettingValueTypeId ?? 0,
            setting.SettingValueTypeCode ?? "UNKNOWN",
            setting.SettingValueTypeName ?? "Unknown",
            setting.ValueSource ?? "unknown",
            setting.IsLocked == true,
            setting.LockSource,
            setting.Description,
            setting.IsSensitive == true,
            setting.AllowedValues?.ToArray() ?? []);

    private static ControlPlaneTenantQuotaUsage MapTenantQuotaUsage(ControlPlaneTenantQuotaUsageDto quota) =>
        new(
            quota.Key ?? "unknown",
            quota.Limit ?? 0,
            quota.Used ?? 0,
            quota.Reserved ?? 0,
            quota.Quarantined ?? 0,
            quota.Available ?? 0,
            quota.ObjectCount ?? 0,
            quota.Provider,
            quota.Source ?? "unknown",
            quota.LastRecalculatedAt);

    private static TenantPlanDraft ToTenantPlanDraft(ControlPlaneTenantPlanDraft draft) =>
        new()
        {
            Key = draft.Key,
            Name = draft.Name,
            Pricing = new TenantPlanPricing
            {
                Amount = (double)draft.Pricing.Amount,
                CurrencyCode = draft.Pricing.CurrencyCode,
                BillingPeriod = draft.Pricing.BillingPeriod
            },
            IsActiveForProvisioning = draft.IsActiveForProvisioning,
            SettingOverrides = draft.SettingOverrides.Select(ToTenantPlanSettingOverride).ToArray(),
            QuotaLimits = draft.QuotaLimits.Select(ToTenantPlanQuotaLimit).ToArray()
        };

    private static TenantPlanSettingOverride ToTenantPlanSettingOverride(
        ControlPlaneTenantPlanSettingOverride setting) =>
        new()
        {
            Key = setting.Key,
            JsonValue = setting.JsonValue,
            IsLocked = setting.IsLocked
        };

    private static TenantPlanQuotaLimit ToTenantPlanQuotaLimit(ControlPlaneTenantPlanQuotaLimit quota) =>
        new()
        {
            Key = quota.Key,
            Limit = quota.Limit
        };

    private static TenantPlanEffectiveConfiguration ToTenantPlanEffectiveConfiguration(
        ControlPlaneTenantPlanEffectiveConfiguration current) =>
        new()
        {
            Settings = current.Settings.Select(ToTenantPlanEffectiveSetting).ToArray()
        };

    private static TenantPlanEffectiveSetting ToTenantPlanEffectiveSetting(
        ControlPlaneTenantPlanEffectiveSetting setting) =>
        new()
        {
            Key = setting.Key,
            JsonValue = setting.JsonValue,
            IsLocked = setting.IsLocked
        };

    private static ControlPlaneDomainSummary MapDomain(ControlPlaneDnsRecordDto record) =>
        new(
            record.Name ?? "unknown",
            record.Purpose ?? record.RecordType ?? "domain",
            record.Status ?? "unknown",
            record.Target,
            record.Guidance);

    private static ControlPlaneOperationStatus MapOperationStatus(ControlPlaneOperationStatusDto status)
    {
        var key = string.IsNullOrWhiteSpace(status.Key) ? "unknown" : status.Key;

        return new ControlPlaneOperationStatus(
            key,
            status.DisplayName ?? key,
            status.Status ?? "unknown",
            status.Severity ?? ControlPlaneSeverity.Neutral,
            status.Message,
            status.Metrics?.Select(MapOperationMetric).ToArray());
    }

    private static ControlPlaneOperationMetric MapOperationMetric(ControlPlaneOperationMetricDto metric)
    {
        var key = string.IsNullOrWhiteSpace(metric.Key) ? "unknown" : metric.Key;

        return new ControlPlaneOperationMetric(
            key,
            metric.DisplayName ?? key,
            metric.Value ?? 0,
            metric.IsCapped == true);
    }

    private static ControlPlaneDeploymentModeTargetOption MapDeploymentModeTargetOption(
        ControlPlaneDeploymentModeTargetOptionDto option)
    {
        var targetMode = string.IsNullOrWhiteSpace(option.TargetMode) ? "unknown" : option.TargetMode;

        return new ControlPlaneDeploymentModeTargetOption(
            targetMode,
            option.Label ?? targetMode,
            option.Description ?? string.Empty,
            option.Allowed == true,
            option.ConfirmationText ?? string.Empty,
            option.BlockingReason,
            option.Remediation);
    }

    private static ControlPlaneDeploymentModeRunbookStep MapDeploymentModeRunbookStep(
        ControlPlaneDeploymentModeRunbookStepDto step)
    {
        var key = string.IsNullOrWhiteSpace(step.Key) ? "unknown" : step.Key;

        return new ControlPlaneDeploymentModeRunbookStep(
            key,
            step.Title ?? key,
            step.Description ?? string.Empty,
            step.Severity ?? ControlPlaneSeverity.Info);
    }

    private static IReadOnlyList<ControlPlaneStatusCard> MapOverviewStatusCards(HalResourceOfControlPlaneOverviewDto overview)
    {
        var cards = new List<ControlPlaneStatusCard>
        {
            new("total-tenants", "Total tenants", (overview.TotalTenantCount ?? 0).ToString(CultureInfo.InvariantCulture)),
            new("active-tenants", "Active tenants", (overview.ActiveTenantCount ?? 0).ToString(CultureInfo.InvariantCulture), ControlPlaneSeverity.Success)
        };

        foreach (var provider in overview.ProviderSummaries ?? [])
        {
            var key = string.IsNullOrWhiteSpace(provider.Key) ? $"provider-{cards.Count}" : $"provider-{provider.Key}";
            cards.Add(new ControlPlaneStatusCard(
                key,
                provider.DisplayName ?? provider.Key ?? "Provider",
                provider.Status ?? (provider.Configured == true ? "Configured" : "Missing"),
                provider.Configured == true ? ControlPlaneSeverity.Success : ControlPlaneSeverity.Warning,
                provider.Message));
        }

        return cards;
    }

    private static IReadOnlyList<ControlPlaneWarning> MapWarnings(IEnumerable? warnings)
    {
        if (warnings is null)
        {
            return [];
        }

        var result = new List<ControlPlaneWarning>();

        foreach (var warning in warnings)
        {
            var code = GetStringProperty(warning, "Code");
            var message = GetStringProperty(warning, "Message");

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            result.Add(new ControlPlaneWarning(
                code,
                message,
                GetStringProperty(warning, "Severity") ?? ControlPlaneSeverity.Warning,
                GetStringProperty(warning, "Remediation")));
        }

        return result;
    }

    private static IReadOnlyDictionary<string, ControlPlaneHalLink> MapLinks(object? links)
    {
        if (links is not IEnumerable entries)
        {
            return ControlPlaneHal.EmptyLinks;
        }

        var mapped = new Dictionary<string, ControlPlaneHalLink>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var rel = GetProperty(entry, "Key") as string;
            var value = GetProperty(entry, "Value");
            var href = GetStringProperty(value, "Href");

            if (string.IsNullOrWhiteSpace(rel) || string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            mapped[rel] = new ControlPlaneHalLink(
                href,
                GetStringProperty(value, "Method"),
                GetStringProperty(value, "Title"),
                GetProperty(value, "Templated") as bool?);
        }

        return mapped.Count == 0 ? ControlPlaneHal.EmptyLinks : mapped;
    }

    private async Task<ControlPlaneCommandResult> SendTenantLifecycleCommandAsync(
        Func<CancellationToken, Task<BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await send(cancellationToken);

            return response.Success == true
                ? ControlPlaneCommandResult.Succeeded(response.Message ?? "Tenant lifecycle updated.")
                : ControlPlaneCommandResult.Failed(
                    response.Message ?? "The control-plane command failed.",
                    response.FailureCode,
                    errors: response.Errors?.ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return CommandFailure(ex);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Control-plane command adapter failed before receiving a response.");
            return ControlPlaneCommandResult.Failed(
                "The control-plane API adapter could not reach the API.",
                "control_plane_api_unavailable");
        }
    }

    private async Task<ControlPlaneResult<T>> SendTenantPlanQueryAsync<TResponse, T>(
        Func<CancellationToken, Task<TResponse>> send,
        Func<TResponse, T> map,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await send(cancellationToken);
            return ControlPlaneResult.Success(map(response));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return ApiFailure<T>(ex);
        }
        catch (Exception ex)
        {
            return UnexpectedFailure<T>(ex);
        }
    }

    private async Task<ControlPlaneCommandResult> SendTenantPlanCommandAsync(
        Func<CancellationToken, Task<BaseCommandResponseOfGuid>> send,
        string successMessage,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await send(cancellationToken);

            return response.Success == true
                ? ControlPlaneCommandResult.Succeeded(response.Message ?? successMessage)
                : ControlPlaneCommandResult.Failed(
                    response.Message ?? failureMessage,
                    response.FailureCode,
                    errors: response.Errors?.ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return CommandFailure(ex);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Control-plane tenant-plan command adapter failed before receiving a response.");
            return ControlPlaneCommandResult.Failed(
                "The control-plane API adapter could not reach the API.",
                "control_plane_api_unavailable");
        }
    }

    private async Task<ControlPlaneCommandResult> SendDeploymentModeTransitionCommandAsync(
        Func<CancellationToken, Task<BaseCommandResponseOfControlPlaneDeploymentModeTransitionDto>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await send(cancellationToken);

            return response.Success == true
                ? ControlPlaneCommandResult.Succeeded(response.Message ?? "Deployment mode transition completed.")
                : ControlPlaneCommandResult.Failed(
                    response.Message ?? "The deployment mode transition failed.",
                    response.FailureCode,
                    errors: response.Errors?.ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiException ex)
        {
            return CommandFailure(ex);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Control-plane deployment mode command adapter failed before receiving a response.");
            return ControlPlaneCommandResult.Failed(
                "The control-plane API adapter could not reach the API.",
                "control_plane_api_unavailable");
        }
    }

    private ControlPlaneResult<T> ApiFailure<T>(ApiException ex)
    {
        var (kind, code, message) = ex.StatusCode switch
        {
            400 or 422 => (ControlPlaneResultKind.ValidationFailed, "control_plane_api_validation_failed", "The control-plane API rejected the request."),
            401 => (ControlPlaneResultKind.Unauthenticated, "control_plane_api_unauthenticated", "Sign in to access the control-plane API."),
            403 => (ControlPlaneResultKind.Forbidden, "control_plane_api_forbidden", "You are not allowed to access the control-plane API."),
            404 => (ControlPlaneResultKind.NotFound, "control_plane_api_not_found", "The control-plane API resource was not found."),
            409 => (ControlPlaneResultKind.Conflict, "control_plane_api_conflict", "The control-plane API request conflicted with the current state."),
            429 => (ControlPlaneResultKind.RateLimited, "control_plane_api_rate_limited", "The control-plane API rate limit was reached."),
            502 or 503 or 504 => (ControlPlaneResultKind.Unavailable, "control_plane_api_unavailable", "The control-plane API is temporarily unavailable."),
            _ => (ControlPlaneResultKind.Failed, "control_plane_api_failed", "The control-plane API request failed.")
        };

        logger.LogWarning("Control-plane API request failed with status {StatusCode}.", ex.StatusCode);
        return ControlPlaneResult.Failure<T>(kind, new ControlPlaneProblem(code, message, ex.StatusCode));
    }

    private ControlPlaneCommandResult CommandFailure(ApiException ex)
    {
        var (code, message) = ex.StatusCode switch
        {
            400 or 422 => ("control_plane_api_validation_failed", "The control-plane API rejected the request."),
            401 => ("control_plane_api_unauthenticated", "Sign in to access the control-plane API."),
            403 => ("control_plane_api_forbidden", "You are not allowed to access the control-plane API."),
            404 => ("control_plane_api_not_found", "The control-plane API resource was not found."),
            409 => ("control_plane_api_conflict", "The control-plane API request conflicted with the current state."),
            429 => ("control_plane_api_rate_limited", "The control-plane API rate limit was reached."),
            502 or 503 or 504 => ("control_plane_api_unavailable", "The control-plane API is temporarily unavailable."),
            _ => ("control_plane_api_failed", "The control-plane API request failed.")
        };

        logger.LogWarning("Control-plane API command failed with status {StatusCode}.", ex.StatusCode);
        return ControlPlaneCommandResult.Failed(message, code, ex.StatusCode);
    }

    private ControlPlaneResult<T> UnexpectedFailure<T>(Exception ex)
    {
        logger.LogWarning(ex, "Control-plane API adapter failed before receiving a response.");
        return ControlPlaneResult.Failure<T>(
            ControlPlaneResultKind.Unavailable,
            new ControlPlaneProblem("control_plane_api_unavailable", "The control-plane API adapter could not reach the API."));
    }

    private static object? GetProperty(object? source, string propertyName) =>
        source?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);

    private static string? GetStringProperty(object? source, string propertyName) =>
        GetProperty(source, propertyName) as string;
}
