// ABOUTME: Cerbos PDP authorization service using HTTP API for policy decisions.
// Calls the Cerbos server's /api/check/resources endpoint without requiring the gRPC SDK NuGet.

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Authorization service that delegates decisions to an external Cerbos PDP via HTTP API.
/// Uses the Cerbos REST API (/api/check/resources) for compatibility without gRPC SDK dependency.
/// Supports both the instance PDP and per-tenant BYO (Bring Your Own) Cerbos endpoints.
/// </summary>
public class CerbosAuthorizationService : IAuthorizationProvider
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CerbosPrincipalBuilder _principalBuilder;
    private readonly IAdminContext _adminContext;
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CerbosAuthorizationService> _logger;
    private readonly CerbosSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CerbosAuthorizationService(
        IHttpClientFactory httpClientFactory,
        CerbosPrincipalBuilder principalBuilder,
        IAdminContext adminContext,
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        IOptions<CerbosSettings> settings,
        ILogger<CerbosAuthorizationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = httpClientFactory.CreateClient("CerbosClient");
        _principalBuilder = principalBuilder;
        _adminContext = adminContext;
        _settingsResolver = settingsResolver;
        _tenantContext = tenantContext;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<bool> IsAllowedAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken cancellationToken = default)
    {
        var checks = new[]
        {
            new AuthorizationCheck(
                resourceKind,
                resourceId,
                action,
                resourceAttributes is null ? null : new Dictionary<string, object>(resourceAttributes))
        };

        var results = await IsAllowedBatchAsync(checks, cancellationToken);
        return results.Count > 0 && results[0];
    }

    public async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default)
    {
        if (checks.Count == 0)
            return [];

        var userId = _adminContext.UserId;
        if (userId == null)
        {
            _logger.LogWarning("Cerbos auth denied: no user id in admin context.");
            return DenyAll(checks.Count);
        }

        var requestId = Guid.NewGuid().ToString();
        var correlationId = Activity.Current?.Id ?? string.Empty;
        var principal = await _principalBuilder.BuildAsync(userId.Value, cancellationToken);
        var resources = BuildResources(checks);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/check/resources",
                new { requestId, principal, resources },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Cerbos auth denied batch: status={StatusCode} requestId={RequestId} correlationId={CorrelationId}",
                    response.StatusCode,
                    requestId,
                    correlationId);
                return DenyAll(checks.Count);
            }

            var result = await response.Content.ReadFromJsonAsync<CerbosCheckResponse>(JsonOptions, cancellationToken);

            return BuildDecisionResults(checks, result, requestId, correlationId);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(
                ex,
                "Cerbos PDP unreachable at {Endpoint}. Denying access for requestId={RequestId} correlationId={CorrelationId}",
                _settings.Endpoint,
                requestId,
                correlationId);
            return DenyAll(checks.Count);
        }
    }

    /// <summary>
    /// Checks permissions against a specific Cerbos PDP endpoint (for BYO tenants).
    /// Creates a temporary HttpClient targeting the given endpoint.
    /// </summary>
    /// <param name="endpointUrl">The BYO Cerbos PDP HTTP URL.</param>
    /// <param name="checks">The authorization checks to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decision results matching check order.</returns>
    public async Task<IReadOnlyList<bool>> IsAllowedBatchWithEndpointAsync(
        string endpointUrl,
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default)
    {
        if (checks.Count == 0)
            return [];

        var userId = _adminContext.UserId;
        if (userId == null)
        {
            _logger.LogWarning("Cerbos auth denied (BYO): no user id in admin context.");
            return DenyAll(checks.Count);
        }

        var requestId = Guid.NewGuid().ToString();
        var correlationId = Activity.Current?.Id ?? string.Empty;
        var principal = await _principalBuilder.BuildAsync(userId.Value, cancellationToken);
        var resources = BuildResources(checks);

        var client = _httpClientFactory.CreateClient("CerbosByoClient");
        client.BaseAddress = new Uri(endpointUrl.TrimEnd('/'));

        var response = await client.PostAsJsonAsync(
            "/api/check/resources",
            new { requestId, principal, resources },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "BYO Cerbos auth denied batch: endpoint={Endpoint} status={StatusCode} requestId={RequestId} correlationId={CorrelationId}",
                endpointUrl,
                response.StatusCode,
                requestId,
                correlationId);
            return DenyAll(checks.Count);
        }

        var result = await response.Content.ReadFromJsonAsync<CerbosCheckResponse>(JsonOptions, cancellationToken);
        return BuildDecisionResults(checks, result, requestId, correlationId);
    }

    private CerbosResourceAction[] BuildResources(IReadOnlyList<AuthorizationCheck> checks)
    {
        var tenantId = _tenantContext.TenantId;

        return checks
            .Select(check =>
            {
                var attr = check.ResourceAttributes is null
                    ? new Dictionary<string, object>()
                    : new Dictionary<string, object>(check.ResourceAttributes);

                // Auto-enrich with tenantId from current context when not explicitly provided.
                // Required for Cerbos derived role evaluation (tenant_admin checks resource.attr.tenantId).
                if (!attr.ContainsKey("tenantId") && tenantId != Guid.Empty)
                    attr["tenantId"] = tenantId.ToString();

                return new CerbosResourceAction
                {
                    Resource = new CerbosResource
                    {
                        Kind = check.ResourceKind,
                        Id = check.ResourceId,
                        Attr = attr,
                        // Scope enables per-tenant policy overrides. When a tenant has custom policies,
                        // Cerbos resolves the most specific scoped policy and falls back to root.
                        Scope = tenantId != Guid.Empty ? tenantId.ToString() : null
                    },
                    Actions = [check.Action]
                };
            })
            .ToArray();
    }

    private IReadOnlyList<bool> BuildDecisionResults(
        IReadOnlyList<AuthorizationCheck> checks,
        CerbosCheckResponse? result,
        string requestId,
        string correlationId)
    {
        var decisions = new bool[checks.Count];

        for (var i = 0; i < checks.Count; i++)
        {
            var check = checks[i];
            var actionResult = result?.Results?.ElementAtOrDefault(i)?.Actions;

            if (actionResult != null && actionResult.TryGetValue(check.Action, out var effect))
            {
                var isAllowed = effect == "EFFECT_ALLOW";
                decisions[i] = isAllowed;
                _logger.LogDebug(
                    "Cerbos decision: effect={Effect} resource={Resource}/{ResourceId} action={Action} requestId={RequestId} correlationId={CorrelationId}",
                    effect,
                    check.ResourceKind,
                    check.ResourceId,
                    check.Action,
                    requestId,
                    correlationId);
                continue;
            }

            _logger.LogWarning(
                "Cerbos decision missing. Default deny for resource={Resource}/{ResourceId} action={Action} requestId={RequestId} correlationId={CorrelationId}",
                check.ResourceKind,
                check.ResourceId,
                check.Action,
                requestId,
                correlationId);
            decisions[i] = false;
        }

        return decisions;
    }

    private static bool[] DenyAll(int count)
    {
        return Enumerable.Repeat(false, count).ToArray();
    }

    public async Task<bool> CheckSettingAccessAsync(
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var attributes = new Dictionary<string, object> { ["settingKey"] = settingKey };
        string resourceKind;

        if (organizationId.HasValue)
        {
            resourceKind = "organization";
            attributes["organizationId"] = organizationId.Value.ToString();
        }
        else if (tenantId.HasValue)
        {
            resourceKind = "tenant_setting";
            attributes["tenantId"] = tenantId.Value.ToString();
            var canOverride = await _settingsResolver.CanOverrideAsync(settingKey, cancellationToken);
            attributes["isLockedByInstance"] = !canOverride;
        }
        else
        {
            resourceKind = "instance_setting";
        }

        return await IsAllowedAsync(resourceKind, settingKey, action, attributes, cancellationToken);
    }
}

/// <summary>
/// Configuration for connecting to the Cerbos PDP server.
/// </summary>
public class CerbosSettings
{
    public const string SectionName = "Cerbos";

    /// <summary>
    /// Whether Cerbos PDP is enabled. When false, uses FallbackAuthorizationService.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The Cerbos PDP HTTP API endpoint (e.g., "http://localhost:3592").
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:3592";
}

// Internal DTOs for Cerbos HTTP API communication

internal class CerbosCheckRequest
{
    public CerbosPrincipal Principal { get; set; } = null!;
    public CerbosResourceAction Resource { get; set; } = null!;
}

/// <summary>
/// Principal DTO for the Cerbos HTTP API. Public so <see cref="CerbosPrincipalBuilder"/> can construct it.
/// </summary>
public class CerbosPrincipal
{
    public string Id { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public Dictionary<string, object> Attr { get; set; } = [];
}

internal class CerbosResource
{
    public string Kind { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public IDictionary<string, object> Attr { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Scope for per-tenant policy overrides. When set, Cerbos resolves the most specific
    /// scoped policy matching this scope and falls back to the root policy.
    /// </summary>
    public string? Scope { get; set; }
}

internal class CerbosResourceAction
{
    public CerbosResource Resource { get; set; } = null!;
    public List<string> Actions { get; set; } = [];
}

internal class CerbosCheckResponse
{
    public List<CerbosResultEntry>? Results { get; set; }
}

internal class CerbosResultEntry
{
    public Dictionary<string, string>? Actions { get; set; }
}
