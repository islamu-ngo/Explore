// ABOUTME: Generates typed Cerbos derived role policies from Permission/RolePermission data and pushes via Admin API.
// ABOUTME: Supports bundle sync (build all → push all), incremental per-role sync, reload broadcast, and policy summary.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Synchronizes authorization policies to Cerbos PDP instances via the Admin API.
/// <para>
/// <b>Build phase</b>: Reads <c>Role</c> and <c>RolePermission</c> tables, generates typed
/// <see cref="CerbosDerivedRolePolicyDocument"/> models for each role with permissions.
/// </para>
/// <para>
/// <b>Push phase</b>: Serializes each policy document and POSTs to the primary Cerbos endpoint.
/// </para>
/// <para>
/// <b>Reload phase</b>: Broadcasts <c>GET /admin/store/reload</c> to all configured endpoints,
/// then invalidates the admin context cache.
/// </para>
/// </summary>
public sealed class PolicySyncService : IPolicySyncService
{
    private readonly HttpClient _httpClient;
    private readonly CerbosAdminApiSettings _settings;
    private readonly IRoleRepository _roleRepository;
    private readonly IAdminCacheInvalidator _cacheInvalidator;
    private readonly ILogger<PolicySyncService> _logger;

    private static readonly JsonSerializerOptions PolicyJsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public PolicySyncService(
        IHttpClientFactory httpClientFactory,
        IOptions<CerbosAdminApiSettings> settings,
        IRoleRepository roleRepository,
        IAdminCacheInvalidator cacheInvalidator,
        ILogger<PolicySyncService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CerbosAdminClient");
        _settings = settings.Value;
        _roleRepository = roleRepository;
        _cacheInvalidator = cacheInvalidator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SyncAllPoliciesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full policy sync to Cerbos");

        try
        {
            // Phase 1: Build all policies in memory
            var (policies, roleCount, totalPermissions) = await BuildAllPoliciesAsync(cancellationToken);

            if (policies.Count == 0)
            {
                _logger.LogInformation("No roles with permissions found. Skipping policy push");
                return;
            }

            // Phase 2: Push each policy document to the primary Cerbos endpoint
            foreach (var policy in policies)
            {
                await PushPolicyAsync(policy, cancellationToken);
            }

            // Phase 3: Broadcast reload + invalidate cache
            await ReloadAllInstancesAsync(cancellationToken);
            _cacheInvalidator.InvalidateAll();

            var hash = ComputeContentHash(policies);
            _logger.LogInformation(
                "Full policy sync completed. Roles={RoleCount} policies={PolicyCount} permissions={PermissionCount} contentHash={Hash}",
                roleCount,
                policies.Count,
                totalPermissions,
                hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Full policy sync failed. Cerbos policies may be stale");
        }
    }

    /// <inheritdoc />
    public async Task SyncRolePoliciesAsync(int roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Syncing policies for role {RoleId}", roleId);

        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role is null)
            {
                _logger.LogWarning("Role {RoleId} not found. Skipping policy sync", roleId);
                return;
            }

            var permissions = await _roleRepository.GetPermissionsForRoleAsync(roleId);
            if (permissions.Count == 0)
            {
                _logger.LogDebug("Role {RoleId} ({MasterCode}) has no permissions. Skipping", roleId, role.MasterCode);
                return;
            }

            var policy = BuildDerivedRolePolicy(role, permissions);
            await PushPolicyAsync(policy, cancellationToken);

            _logger.LogInformation(
                "Synced policies for role {RoleId} ({MasterCode}) with {Count} permissions",
                roleId, role.MasterCode, permissions.Count);
        }
        catch (Exception ex)
        {
            // Resilient: log but don't fail the calling command
            _logger.LogError(ex, "Failed to sync policies for role {RoleId}. Cerbos policies may be stale", roleId);
        }
    }

    /// <inheritdoc />
    public async Task ReloadAllInstancesAsync(CancellationToken cancellationToken = default)
    {
        if (_settings.Endpoints.Count == 0)
        {
            _logger.LogDebug("No Cerbos endpoints configured for reload broadcast");
            return;
        }

        _logger.LogInformation("Broadcasting reload to {Count} Cerbos instance(s)", _settings.Endpoints.Count);

        var tasks = _settings.Endpoints.Select(endpoint =>
            ReloadInstanceAsync(endpoint, cancellationToken));

        var results = await Task.WhenAll(tasks);
        var succeeded = results.Count(r => r);
        var failed = results.Length - succeeded;

        if (failed > 0)
        {
            _logger.LogWarning("Reload broadcast: {Succeeded} succeeded, {Failed} failed", succeeded, failed);
        }
        else
        {
            _logger.LogInformation("Reload broadcast: all {Count} instances reloaded", succeeded);
        }
    }

    /// <inheritdoc />
    public async Task<PolicyPackageInfo> GetPolicySummaryAsync(CancellationToken cancellationToken = default)
    {
        var (policies, roleCount, totalPermissions) = await BuildAllPoliciesAsync(cancellationToken);
        var hash = ComputeContentHash(policies);

        return new PolicyPackageInfo(
            RoleCount: roleCount,
            PolicyCount: policies.Count,
            TotalPermissionCount: totalPermissions,
            ContentHash: hash,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    #region Build

    /// <summary>
    /// Reads all roles and their permissions, builds a typed policy document for each role
    /// that has at least one permission. Returns the built policies plus summary counts.
    /// </summary>
    private async Task<(IReadOnlyList<CerbosDerivedRolePolicyDocument> Policies, int RoleCount, int TotalPermissions)>
        BuildAllPoliciesAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync();
        var policies = new List<CerbosDerivedRolePolicyDocument>();
        var totalPermissions = 0;

        foreach (var role in roles)
        {
            var permissions = await _roleRepository.GetPermissionsForRoleAsync(role.Id);
            if (permissions.Count == 0)
                continue;

            policies.Add(BuildDerivedRolePolicy(role, permissions));
            totalPermissions += permissions.Count;
        }

        return (policies, roles.Count, totalPermissions);
    }

    /// <summary>
    /// Generates a typed Cerbos derived role policy document from a role and its permissions.
    /// The derived role name is <c>dynamic_{MasterCode}</c> with definitions grouped by resource kind.
    /// Each definition matches authenticated users whose <c>roles</c> attribute contains the role's MasterCode.
    /// </summary>
    private static CerbosDerivedRolePolicyDocument BuildDerivedRolePolicy(
        Domain.Role role,
        IReadOnlyList<Domain.Permission> permissions)
    {
        var permsByResource = permissions
            .GroupBy(p => p.ResourceKind)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Action).ToList());

        return new CerbosDerivedRolePolicyDocument
        {
            DerivedRoles = new CerbosDerivedRolesSpec
            {
                Name = $"dynamic_{role.MasterCode.Replace('.', '_')}",
                Definitions = permsByResource.Select(kvp => new CerbosDerivedRoleDefinition
                {
                    Name = $"{role.MasterCode}_{kvp.Key}",
                    ParentRoles = ["authenticated_user"],
                    Condition = new CerbosDerivedRoleCondition
                    {
                        Match = new CerbosDerivedRoleMatch
                        {
                            All = new CerbosDerivedRoleMatchAll
                            {
                                Of =
                                [
                                    new CerbosDerivedRoleExpression
                                    {
                                        Expr = $"P.attr.roles.exists(r, r == \"{role.MasterCode}\")"
                                    }
                                ]
                            }
                        }
                    }
                }).ToArray()
            }
        };
    }

    #endregion

    #region Push & Reload

    private async Task PushPolicyAsync(CerbosDerivedRolePolicyDocument policy, CancellationToken cancellationToken)
    {
        if (_settings.Endpoints.Count == 0)
        {
            _logger.LogDebug("No Cerbos endpoints configured for policy push");
            return;
        }

        var endpoint = _settings.Endpoints[0]; // Push to primary endpoint
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/admin/policy");
        AddBasicAuth(request);
        request.Content = JsonContent.Create(policy, options: PolicyJsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to push policy to Cerbos at {Endpoint}: {StatusCode} - {Body}",
                endpoint, response.StatusCode, body);
            throw new InvalidOperationException($"Cerbos policy push failed: {response.StatusCode}");
        }
    }

    private async Task<bool> ReloadInstanceAsync(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/admin/store/reload?wait=true");
            AddBasicAuth(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Reloaded Cerbos instance at {Endpoint}", endpoint);
                return true;
            }

            _logger.LogWarning("Failed to reload Cerbos instance at {Endpoint}: {StatusCode}", endpoint, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to contact Cerbos instance at {Endpoint} for reload", endpoint);
            return false;
        }
    }

    private void AddBasicAuth(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_settings.AdminUsername))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.AdminUsername}:{_settings.AdminPassword}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    #endregion

    #region Hashing

    /// <summary>
    /// Computes a SHA-256 content hash over the serialized policy bundle.
    /// Used for staleness detection — if the hash changes, policies have diverged.
    /// </summary>
    private static string ComputeContentHash(IReadOnlyList<CerbosDerivedRolePolicyDocument> policies)
    {
        if (policies.Count == 0)
            return string.Empty;

        var json = JsonSerializer.Serialize(policies, PolicyJsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion
}
