// ABOUTME: Cerbos PDP authorization service using the official gRPC SDK for policy decisions.
// ABOUTME: Uses Cerbos.Sdk CheckResourcesAsync, prefers AuthorizationCheck.Scope over ambient tenant context.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cerbos.Api.V1.Effect;
using Cerbos.Sdk;
using Cerbos.Sdk.Builder;
using Cerbos.Sdk.Response;
using Cerbos.Sdk.Utility;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CerbosCheckResourcesRequest = Cerbos.Sdk.Builder.CheckResourcesRequest;
using CerbosPrincipalBuilder = Explore.Infrastructure.Services.CerbosPrincipalBuilder;
using CerbosResourceEntry = Cerbos.Sdk.Builder.ResourceEntry;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Authorization service that delegates decisions to an external Cerbos PDP via the official gRPC SDK.
/// Supports both the instance PDP and per-tenant BYO (Bring Your Own) Cerbos endpoints.
/// No admin credentials are needed — runtime CheckResources is unauthenticated.
/// </summary>
public class CerbosAuthorizationService : IAuthorizationProvider
{
    private readonly ICerbosClient _client;
    private readonly CerbosPrincipalBuilder _principalBuilder;
    private readonly IAdminContext _adminContext;
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICerbosClientFactory _clientFactory;
    private readonly ILogger<CerbosAuthorizationService> _logger;
    private readonly CerbosSettings _settings;

    public CerbosAuthorizationService(
        ICerbosClient client,
        CerbosPrincipalBuilder principalBuilder,
        IAdminContext adminContext,
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        ICerbosClientFactory clientFactory,
        IOptions<CerbosSettings> settings,
        ILogger<CerbosAuthorizationService> logger)
    {
        _client = client;
        _principalBuilder = principalBuilder;
        _adminContext = adminContext;
        _resolver = resolver;
        _tenantContext = tenantContext;
        _clientFactory = clientFactory;
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

        return await ExecuteCheckAsync(_client, _settings.GrpcEndpoint, checks, cancellationToken);
    }

    /// <summary>
    /// Checks permissions against a specific Cerbos PDP endpoint (for BYO tenants).
    /// Uses <see cref="ICerbosClientFactory"/> to get a cached gRPC client for the BYO endpoint.
    /// </summary>
    public async Task<IReadOnlyList<bool>> IsAllowedBatchWithEndpointAsync(
        string endpointUrl,
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken = default)
    {
        if (checks.Count == 0)
            return [];

        var byoClient = _clientFactory.GetOrCreate(endpointUrl);
        return await ExecuteCheckAsync(byoClient, endpointUrl, checks, cancellationToken);
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
            var metadata = await _resolver.ResolveWithMetadataAsync(settingKey, new SettingContext(), cancellationToken);
            attributes["isLockedByInstance"] = metadata?.IsLocked == true;
        }
        else
        {
            resourceKind = "instance_setting";
        }

        return await IsAllowedAsync(resourceKind, settingKey, action, attributes, cancellationToken);
    }

    private async Task<IReadOnlyList<bool>> ExecuteCheckAsync(
        ICerbosClient client,
        string endpointLabel,
        IReadOnlyList<AuthorizationCheck> checks,
        CancellationToken cancellationToken)
    {
        var userId = _adminContext.UserId;
        if (userId == null)
        {
            _logger.LogWarning("Cerbos auth denied: no user id in admin context");
            return DenyAll(checks.Count);
        }

        var requestId = RequestId.Generate();
        var correlationId = Activity.Current?.Id ?? string.Empty;
        var principal = await _principalBuilder.BuildSdkPrincipalAsync(userId.Value, cancellationToken);
        var resourceEntries = BuildResourceEntries(checks);

        _logger.LogDebug(
            "Cerbos gRPC batch request: {CheckCount} checks, requestId={RequestId} correlationId={CorrelationId}",
            checks.Count, requestId, correlationId);

        try
        {
            var request = CerbosCheckResourcesRequest
                .NewInstance()
                .WithRequestId(requestId)
                .WithPrincipal(principal)
                .WithResourceEntries(resourceEntries);

            var response = await client.CheckResourcesAsync(request, null);
            return BuildDecisionResults(checks, response, requestId, correlationId);
        }
        catch (Exception ex) when (ex is Grpc.Core.RpcException or OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Cerbos PDP unreachable at {Endpoint}. Denying access for requestId={RequestId} correlationId={CorrelationId}",
                endpointLabel, requestId, correlationId);
            return DenyAll(checks.Count);
        }
    }

    private CerbosResourceEntry[] BuildResourceEntries(IReadOnlyList<AuthorizationCheck> checks)
    {
        var ambientTenantId = _tenantContext.TenantId;

        return checks
            .Select(check =>
            {
                // Resolve tenant ID: prefer explicit scope from the check, fall back to ambient tenant context.
                var effectiveTenantId = !string.IsNullOrEmpty(check.Scope?.TenantId)
                    ? check.Scope.TenantId
                    : ambientTenantId != Guid.Empty
                        ? ambientTenantId.ToString()
                        : null;

                var entry = CerbosResourceEntry
                    .NewInstance(check.ResourceKind, check.ResourceId)
                    .WithActions(check.Action);

                // Scope enables per-tenant policy overrides. When a tenant has custom policies,
                // Cerbos resolves the most specific scoped policy and falls back to root.
                if (effectiveTenantId is not null)
                    entry = entry.WithScope(effectiveTenantId);

                // Map resource attributes to Cerbos AttributeValue types
                if (check.ResourceAttributes is not null)
                {
                    foreach (var (key, value) in check.ResourceAttributes)
                        entry = entry.WithAttribute(key, ToAttributeValue(value));
                }

                // Auto-enrich with tenantId when not explicitly provided in resource attributes.
                // Required for Cerbos derived role evaluation (tenant_admin checks resource.attr.tenantId).
                if (effectiveTenantId is not null &&
                    (check.ResourceAttributes is null || !check.ResourceAttributes.ContainsKey("tenantId")))
                {
                    entry = entry.WithAttribute("tenantId", AttributeValue.StringValue(effectiveTenantId));
                }

                return entry;
            })
            .ToArray();
    }

    private IReadOnlyList<bool> BuildDecisionResults(
        IReadOnlyList<AuthorizationCheck> checks,
        CheckResourcesResponse response,
        string requestId,
        string correlationId)
    {
        var decisions = new bool[checks.Count];

        for (var i = 0; i < checks.Count; i++)
        {
            var check = checks[i];
            var resultEntry = response.Find(check.ResourceId);

            if (resultEntry is not null && resultEntry.Actions.TryGetValue(check.Action, out var effect))
            {
                var isAllowed = effect == Effect.Allow;
                decisions[i] = isAllowed;
                _logger.LogDebug(
                    "Cerbos decision: effect={Effect} resource={Resource}/{ResourceId} action={Action} requestId={RequestId} correlationId={CorrelationId}",
                    effect, check.ResourceKind, check.ResourceId, check.Action, requestId, correlationId);
                continue;
            }

            _logger.LogWarning(
                "Cerbos decision missing. Default deny for resource={Resource}/{ResourceId} action={Action} requestId={RequestId} correlationId={CorrelationId}",
                check.ResourceKind, check.ResourceId, check.Action, requestId, correlationId);
            decisions[i] = false;
        }

        return decisions;
    }

    private static bool[] DenyAll(int count)
    {
        return Enumerable.Repeat(false, count).ToArray();
    }

    /// <summary>
    /// Converts a CLR object to the Cerbos SDK <see cref="AttributeValue"/> type.
    /// </summary>
    internal static AttributeValue ToAttributeValue(object? value) => value switch
    {
        null => AttributeValue.NullValue(),
        bool b => AttributeValue.BoolValue(b),
        int n => AttributeValue.DoubleValue(n),
        long n => AttributeValue.DoubleValue(n),
        double d => AttributeValue.DoubleValue(d),
        float f => AttributeValue.DoubleValue(f),
        string s => AttributeValue.StringValue(s),
        IDictionary<string, object> dict => AttributeValue.MapValue(
            dict.ToDictionary(kvp => kvp.Key, kvp => ToAttributeValue(kvp.Value))),
        IEnumerable<object> list => AttributeValue.ListValue(
            list.Select(ToAttributeValue).ToArray()),
        _ => AttributeValue.StringValue(value.ToString() ?? string.Empty)
    };
}

/// <summary>
/// Configuration for connecting to the Cerbos PDP server via gRPC.
/// Bound from the "Cerbos" configuration section in appsettings.json.
/// </summary>
public class CerbosSettings
{
    public const string SectionName = "Cerbos";

    /// <summary>
    /// The Cerbos PDP gRPC endpoint (e.g., "https://cerbosgrpc.openislamu.org:443" for TLS,
    /// "http://localhost:3593" for plaintext). Protocol prefix determines TLS behavior.
    /// </summary>
    public string GrpcEndpoint { get; set; } = "http://localhost:3593";

    /// <summary>
    /// When true, uses plaintext gRPC (no TLS). Required for local development with
    /// <c>http://</c> endpoints. Production deployments should use TLS (<c>https://</c>)
    /// and leave this false.
    /// </summary>
    public bool PlaintextMode { get; set; } = true;
}

/// <summary>
/// Factory for creating and caching Cerbos gRPC clients per endpoint.
/// Used for BYO (Bring Your Own) Cerbos endpoints where each tenant may have
/// a different PDP. gRPC channels are long-lived and thread-safe, so caching is optimal.
/// </summary>
public interface ICerbosClientFactory
{
    ICerbosClient GetOrCreate(string grpcEndpoint);
}

/// <summary>
/// Thread-safe factory that caches <see cref="ICerbosClient"/> instances per gRPC endpoint.
/// </summary>
public class CerbosClientFactory : ICerbosClientFactory
{
    private readonly ConcurrentDictionary<string, ICerbosClient> _clients = new();
    private readonly ILogger<CerbosClientFactory> _logger;

    public CerbosClientFactory(ILogger<CerbosClientFactory> logger)
    {
        _logger = logger;
    }

    public ICerbosClient GetOrCreate(string grpcEndpoint)
    {
        return _clients.GetOrAdd(grpcEndpoint, endpoint =>
        {
            _logger.LogInformation("Creating Cerbos gRPC client for BYO endpoint: {Endpoint}", endpoint);
            var builder = CerbosClientBuilder.ForTarget(endpoint);

            if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                builder = builder.WithPlaintext();

            return builder.Build();
        });
    }
}
