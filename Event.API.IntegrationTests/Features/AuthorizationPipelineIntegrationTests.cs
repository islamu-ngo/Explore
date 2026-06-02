// ABOUTME: Authorization pipeline integration tests using real Keycloak JWTs and real Cerbos PDP decisions.
// ABOUTME: Validates that different user roles get different authorization results from the Cerbos container.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using FluentAssertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// End-to-end authorization pipeline tests that validate the complete chain:
/// Keycloak → API auth middleware → IAuthorizationProvider → Cerbos PDP → response.
///
/// Uses a <see cref="RoleAwareCerbosProvider"/> that queries the containerized Cerbos
/// HTTP API with role-aware principals, exercising real policy decisions for each user.
/// </summary>
[Category(TestCategories.Security)]
[ClassDataSource<SecurityInfrastructureFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("SecurityInfra")]
public class AuthorizationPipelineIntegrationTests : IAsyncDisposable
{
    private readonly SecurityInfrastructureFixture _infra;

    private readonly SecurityWebApplicationFactory _instanceAdminFactory;
    private readonly HttpClient _instanceAdminClient;

    private readonly SecurityWebApplicationFactory _regularUserFactory;
    private readonly HttpClient _regularUserClient;

    private readonly HttpClient _cerbosHttpClient;

    public AuthorizationPipelineIntegrationTests(SecurityInfrastructureFixture infra)
    {
        _infra = infra;
        _cerbosHttpClient = new HttpClient { BaseAddress = new Uri(infra.CerbosHttpEndpoint) };

        var instanceAdminProvider = new RoleAwareCerbosProvider(
            infra.CerbosHttpEndpoint,
            isInstanceAdmin: true,
            tenantMemberships: new Dictionary<string, string>(),
            orgMemberships: new Dictionary<string, string>());

        _instanceAdminFactory = new SecurityWebApplicationFactory(
            infra.KeycloakAuthority,
            infra.KeycloakMetadataAddress,
            infra.CerbosGrpcEndpoint)
        {
            AuthorizationProviderOverride = instanceAdminProvider
        };
        _instanceAdminClient = _instanceAdminFactory.CreateClient();

        var regularUserProvider = new RoleAwareCerbosProvider(
            infra.CerbosHttpEndpoint,
            isInstanceAdmin: false,
            tenantMemberships: new Dictionary<string, string>(),
            orgMemberships: new Dictionary<string, string>());

        _regularUserFactory = new SecurityWebApplicationFactory(
            infra.KeycloakAuthority,
            infra.KeycloakMetadataAddress,
            infra.CerbosGrpcEndpoint)
        {
            AuthorizationProviderOverride = regularUserProvider
        };
        _regularUserClient = _regularUserFactory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _instanceAdminClient.Dispose();
        _regularUserClient.Dispose();
        _cerbosHttpClient.Dispose();
        await _instanceAdminFactory.DisposeAsync();
        await _regularUserFactory.DisposeAsync();
    }

    #region Instance Admin — Full Access

    [Test]
    public async Task InstanceAdmin_GetEvents_ShouldReturnOk()
    {
        var token = await _infra.TokenClient.GetAdminTokenAsync();
        using var request = CreateGetRequest("/api/event", token);

        var response = await _instanceAdminClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Regular User — Read Access

    [Test]
    public async Task RegularUser_GetEvents_ShouldReturnOk()
    {
        var token = await _infra.TokenClient.GetUserTokenAsync();
        using var request = CreateGetRequest("/api/event", token);

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Unauthenticated — Public Read, Denied Write

    [Test]
    public async Task Unauthenticated_GetEvents_ShouldReturnOk()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/event");

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "public GET endpoints should be accessible without authentication");
    }

    [Test]
    public async Task Unauthenticated_CreateEvent_ShouldReturnUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/event")
        {
            Content = JsonContent.Create(new { })
        };

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Unauthenticated_CreateOrganization_ShouldReturnUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/organization")
        {
            Content = JsonContent.Create(new
            {
                FullName = "Unauth Org",
                Email = "unauth@test.islamu.org",
                Country = "Belgium",
                City = "Brussels",
                Address = "Test Street 1",
                Postcode = 1000
            })
        };

        var response = await _regularUserClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Cerbos Decision Verification

    [Test]
    public async Task CerbosPDP_InstanceAdmin_ShouldAllowAllEventActions()
    {
        var result = await CheckCerbosDecision(
            isInstanceAdmin: true,
            tenantMemberships: new Dictionary<string, string>(),
            orgMemberships: new Dictionary<string, string>(),
            resourceKind: ResourceKinds.Event,
            actions: ["view", "create", "update", "delete"]);

        result.Should().ContainValues("EFFECT_ALLOW", "EFFECT_ALLOW", "EFFECT_ALLOW", "EFFECT_ALLOW");
    }

    [Test]
    public async Task CerbosPDP_RegularUser_ShouldOnlyViewEvents()
    {
        var result = await CheckCerbosDecision(
            isInstanceAdmin: false,
            tenantMemberships: new Dictionary<string, string>(),
            orgMemberships: new Dictionary<string, string>(),
            resourceKind: ResourceKinds.Event,
            actions: ["view", "create", "update", "delete"]);

        result["view"].Should().Be("EFFECT_ALLOW");
        result["create"].Should().Be("EFFECT_DENY");
        result["update"].Should().Be("EFFECT_DENY");
        result["delete"].Should().Be("EFFECT_DENY");
    }

    [Test]
    public async Task CerbosPDP_TenantAdmin_ShouldManageOwnTenantOnly()
    {
        var ownTenantResult = await CheckCerbosDecision(
            isInstanceAdmin: false,
            tenantMemberships: new Dictionary<string, string> { ["tenant-1"] = "admin" },
            orgMemberships: new Dictionary<string, string>(),
            resourceKind: ResourceKinds.Event,
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update"]);

        ownTenantResult.Values.Should().OnlyContain(v => v == "EFFECT_ALLOW",
            "tenant admin should be allowed all actions on events in own tenant");

        var otherTenantResult = await CheckCerbosDecision(
            isInstanceAdmin: false,
            tenantMemberships: new Dictionary<string, string> { ["tenant-1"] = "admin" },
            orgMemberships: new Dictionary<string, string>(),
            resourceKind: ResourceKinds.Event,
            resourceAttrs: new { tenantId = "tenant-2", organizationId = "org-2" },
            actions: ["create", "update", "delete"]);

        otherTenantResult.Values.Should().OnlyContain(v => v == "EFFECT_DENY",
            "tenant admin should be denied mutations on events in other tenant");
    }

    [Test]
    public async Task CerbosPDP_OrgAdmin_ShouldManageOwnOrgOnly()
    {
        var ownOrgResult = await CheckCerbosDecision(
            isInstanceAdmin: false,
            tenantMemberships: new Dictionary<string, string>(),
            orgMemberships: new Dictionary<string, string> { ["org-1"] = "admin" },
            resourceKind: ResourceKinds.Event,
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-1" },
            actions: ["view", "create", "update"]);

        ownOrgResult.Values.Should().OnlyContain(v => v == "EFFECT_ALLOW");

        var otherOrgResult = await CheckCerbosDecision(
            isInstanceAdmin: false,
            tenantMemberships: new Dictionary<string, string>(),
            orgMemberships: new Dictionary<string, string> { ["org-1"] = "admin" },
            resourceKind: ResourceKinds.Event,
            resourceAttrs: new { tenantId = "tenant-1", organizationId = "org-other" },
            actions: ["create", "update", "delete"]);

        otherOrgResult.Values.Should().OnlyContain(v => v == "EFFECT_DENY",
            "org admin should be denied mutations on events in other org");
    }

    #endregion

    #region Helpers

    private static HttpRequestMessage CreateGetRequest(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<Dictionary<string, string>> CheckCerbosDecision(
        bool isInstanceAdmin,
        Dictionary<string, string> tenantMemberships,
        Dictionary<string, string> orgMemberships,
        string resourceKind,
        string[] actions,
        object? resourceAttrs = null)
    {
        var attrs = resourceAttrs ?? new { tenantId = "tenant-1", organizationId = "org-1" };

        var payload = new
        {
            requestId = Guid.NewGuid().ToString(),
            includeMeta = false,
            principal = new
            {
                id = "pipeline-test-principal",
                roles = new[] { "islamuevent_authenticated_user" },
                attr = new { isInstanceAdmin, tenantMemberships, orgMemberships }
            },
            resources = new[]
            {
                new
                {
                    resource = new { kind = resourceKind, id = "pipeline-test-resource", attr = attrs },
                    actions
                }
            }
        };

        var response = await _cerbosHttpClient.PostAsJsonAsync("/api/check/resources", payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("results");
        var firstResult = results.EnumerateArray().First();
        var actionsResult = firstResult.GetProperty("actions");

        var map = new Dictionary<string, string>();
        foreach (var prop in actionsResult.EnumerateObject())
        {
            map[prop.Name] = prop.Value.GetString() ?? "UNKNOWN";
        }

        return map;
    }

    #endregion

    /// <summary>
    /// IAuthorizationProvider that delegates to the containerized Cerbos HTTP API
    /// with a fixed role configuration. Each instance represents one user persona
    /// (instance admin, regular user, etc.) and queries real policy decisions.
    /// </summary>
    private sealed class RoleAwareCerbosProvider : IAuthorizationProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _isInstanceAdmin;
        private readonly Dictionary<string, string> _tenantMemberships;
        private readonly Dictionary<string, string> _orgMemberships;

        public RoleAwareCerbosProvider(
            string cerbosHttpEndpoint,
            bool isInstanceAdmin,
            Dictionary<string, string> tenantMemberships,
            Dictionary<string, string> orgMemberships)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(cerbosHttpEndpoint) };
            _isInstanceAdmin = isInstanceAdmin;
            _tenantMemberships = tenantMemberships;
            _orgMemberships = orgMemberships;
        }

        public async Task<bool> IsAllowedAsync(
            string resourceKind,
            string resourceId,
            string action,
            IDictionary<string, object>? resourceAttributes = null,
            CancellationToken cancellationToken = default)
        {
            var results = await IsAllowedBatchAsync(
                [new AuthorizationCheck(resourceKind, resourceId, action,
                    resourceAttributes is not null
                        ? new Dictionary<string, object>(resourceAttributes)
                        : null)],
                cancellationToken);

            return results[0];
        }

        public async Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
            IReadOnlyList<AuthorizationCheck> checks,
            CancellationToken cancellationToken = default)
        {
            var resources = checks.Select(c =>
            {
                var attrs = BuildResourceAttrs(c);
                return new
                {
                    resource = new { kind = c.ResourceKind, id = c.ResourceId, attr = attrs },
                    actions = new[] { c.Action }
                };
            }).ToArray();

            var payload = new
            {
                requestId = Guid.NewGuid().ToString(),
                includeMeta = false,
                principal = new
                {
                    id = "pipeline-principal",
                    roles = new[] { "islamuevent_authenticated_user" },
                    attr = (object)new
                    {
                        isInstanceAdmin = _isInstanceAdmin,
                        tenantMemberships = _tenantMemberships,
                        orgMemberships = _orgMemberships
                    }
                },
                resources
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/check/resources", payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");

            var boolResults = new List<bool>();
            foreach (var resultEntry in results.EnumerateArray())
            {
                var actions = resultEntry.GetProperty("actions");
                var firstAction = actions.EnumerateObject().FirstOrDefault();
                boolResults.Add(firstAction.Value.GetString() == "EFFECT_ALLOW");
            }

            return boolResults;
        }

        public Task<bool> CheckSettingAccessAsync(
            string settingKey, string action,
            Guid? tenantId = null, Guid? organizationId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        private static object BuildResourceAttrs(AuthorizationCheck check)
        {
            if (check.ResourceAttributes is null)
            {
                return new { tenantId = "tenant-1", organizationId = "org-1" };
            }

            var dict = new Dictionary<string, object>();
            foreach (var kvp in check.ResourceAttributes)
            {
                dict[kvp.Key] = kvp.Value;
            }
            if (!dict.ContainsKey("tenantId")) dict["tenantId"] = "tenant-1";
            if (!dict.ContainsKey("organizationId")) dict["organizationId"] = "org-1";
            return dict;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
