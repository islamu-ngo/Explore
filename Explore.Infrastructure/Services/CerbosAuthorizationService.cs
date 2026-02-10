// ABOUTME: Cerbos PDP authorization service using HTTP API for policy decisions.
// Calls the Cerbos server's /api/check/resources endpoint without requiring the gRPC SDK NuGet.

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
/// Falls back to the <see cref="FallbackAuthorizationService"/> pattern if Cerbos is unreachable.
/// </summary>
public class CerbosAuthorizationService : ICerbosAuthorizationService
{
    private readonly HttpClient _httpClient;
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
        IAdminContext adminContext,
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        IOptions<CerbosSettings> settings,
        ILogger<CerbosAuthorizationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CerbosClient");
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
        var userId = _adminContext.UserId;
        if (userId == null)
            return false;

        // Build principal with admin context attributes
        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(cancellationToken);
        var adminTenantIds = await _adminContext.GetAdminTenantIdsAsync(cancellationToken);
        var adminOrgIds = await _adminContext.GetAdminOrganizationIdsAsync(cancellationToken);

        var roles = new List<string> { "user" };
        if (isInstanceAdmin) roles.Add("admin");

        var principalAttrs = new Dictionary<string, object>
        {
            ["isInstanceAdmin"] = isInstanceAdmin,
            ["tenants"] = adminTenantIds,
            ["orgs"] = adminOrgIds
        };

        var request = new CerbosCheckRequest
        {
            Principal = new CerbosPrincipal
            {
                Id = userId.Value.ToString(),
                Roles = roles,
                Attr = principalAttrs
            },
            Resource = new CerbosResourceAction
            {
                Resource = new CerbosResource
                {
                    Kind = resourceKind,
                    Id = resourceId,
                    Attr = resourceAttributes ?? new Dictionary<string, object>()
                },
                Actions = [action]
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/check/resources",
                new { requestId = Guid.NewGuid().ToString(), principal = request.Principal, resources = new[] { request.Resource } },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Cerbos returned {StatusCode}, falling back to deny", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<CerbosCheckResponse>(JsonOptions, cancellationToken);
            var actionResult = result?.Results?.FirstOrDefault()?.Actions;

            if (actionResult != null && actionResult.TryGetValue(action, out var effect))
            {
                var isAllowed = effect == "EFFECT_ALLOW";
                _logger.LogDebug("Cerbos: {Effect} for {Resource}/{Action}", effect, resourceKind, action);
                return isAllowed;
            }

            _logger.LogWarning("Cerbos: no result for action {Action} on {Resource}", action, resourceKind);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Cerbos PDP unreachable at {Endpoint}, denying access", _settings.Endpoint);
            return false;
        }
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
            attributes["organizationId"] = organizationId.Value;
        }
        else if (tenantId.HasValue)
        {
            resourceKind = "tenant_setting";
            attributes["tenantId"] = tenantId.Value;
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

internal class CerbosPrincipal
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
