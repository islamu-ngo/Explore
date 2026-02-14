// ABOUTME: Generates Cerbos policies from Permission/RolePermission data and pushes via Admin API.
// ABOUTME: Broadcasts reload commands to all Cerbos instances for immediate consistency.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Synchronizes authorization policies to Cerbos PDP instances via the Admin API.
/// Reads Permission and RolePermission tables to generate derived role and resource policies,
/// pushes them to the Cerbos PostgreSQL store, and broadcasts reload to all instances.
/// After sync, invalidates AdminContext cache to ensure fresh authorization decisions.
/// </summary>
public sealed class PolicySyncService : IPolicySyncService
{
    private readonly HttpClient _httpClient;
    private readonly CerbosAdminApiSettings _settings;
    private readonly IRoleRepository _roleRepository;
    private readonly IAdminCacheInvalidator _cacheInvalidator;
    private readonly ILogger<PolicySyncService> _logger;

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

    public async Task SyncAllPoliciesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full policy sync to Cerbos");

        try
        {
            var roles = await _roleRepository.GetAllAsync();
            var syncedCount = 0;

            foreach (var role in roles)
            {
                await SyncRolePoliciesAsync(role.Id, cancellationToken);
                syncedCount++;
            }

            await ReloadAllInstancesAsync(cancellationToken);

            // Invalidate cached admin authority profiles so next authorization check uses fresh data
            _cacheInvalidator.InvalidateAll();

            _logger.LogInformation("Full policy sync completed. Synced {Count} roles", syncedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Full policy sync failed. Cerbos policies may be stale");
        }
    }

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

            // Generate derived role policy for this role
            var derivedRolePolicy = GenerateDerivedRolePolicy(role, permissions);

            // Push to first available Cerbos endpoint
            await PushPolicyAsync(derivedRolePolicy, cancellationToken);

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

    private async Task PushPolicyAsync(object policy, CancellationToken cancellationToken)
    {
        if (_settings.Endpoints.Count == 0)
        {
            _logger.LogDebug("No Cerbos endpoints configured for policy push");
            return;
        }

        var endpoint = _settings.Endpoints[0]; // Push to primary endpoint
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/admin/policy");
        AddBasicAuth(request);
        request.Content = JsonContent.Create(policy);

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

    private void AddBasicAuth(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_settings.AdminUsername))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.AdminUsername}:{_settings.AdminPassword}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    /// <summary>
    /// Generates a Cerbos derived role policy from role permissions.
    /// The derived role name matches the Role.MasterCode (e.g., "org.editor").
    /// </summary>
    private static object GenerateDerivedRolePolicy(
        Domain.Role role,
        IReadOnlyList<Domain.Permission> permissions)
    {
        // Group permissions by resource kind for condition building
        var permsByResource = permissions
            .GroupBy(p => p.ResourceKind)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Action).ToList());

        // Build Cerbos derived role definition
        // Format: https://docs.cerbos.dev/cerbos/latest/policies/derived_roles
        return new
        {
            apiVersion = "api.cerbos.dev/v1",
            derivedRoles = new
            {
                name = $"dynamic_{role.MasterCode.Replace('.', '_')}",
                definitions = permsByResource.Select(kvp => new
                {
                    name = $"{role.MasterCode}_{kvp.Key}",
                    parentRoles = new[] { "authenticated_user" },
                    condition = new
                    {
                        match = new
                        {
                            all = new
                            {
                                of = new[]
                                {
                                    new
                                    {
                                        expr = $"P.attr.roles.exists(r, r == \"{role.MasterCode}\")"
                                    }
                                }
                            }
                        }
                    }
                }).ToArray()
            }
        };
    }
}
