// ABOUTME: Host-side adapter that connects the shared control-plane RCL contracts to the generated BFF API client.
// ABOUTME: Keeps API transport, error translation, and generated DTO mapping out of the host-neutral RCL.

using System.Collections;
using System.Globalization;
using System.Reflection;
using Event.ControlPlane.Client.Contracts;
using Event.ControlPlane.Client.Services;
using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services.ControlPlane;

public sealed class ControlPlaneApiAdapter(
    IEventApiClient apiClient,
    ILogger<ControlPlaneApiAdapter> logger)
    : IControlPlaneOverviewService, IControlPlaneTenantService, IControlPlaneDomainService, IControlPlaneOperationsService
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

            return ControlPlaneResult.Success(new ControlPlaneDomainList(
                records,
                MapLinks(domains._links)));
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

    private static ControlPlaneDomainSummary MapDomain(ControlPlaneDnsRecordDto record)
    {
        return new ControlPlaneDomainSummary(
            record.Name ?? "unknown",
            record.Purpose ?? record.RecordType ?? "domain",
            record.Status ?? "unknown",
            record.Target,
            record.Guidance);
    }

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
