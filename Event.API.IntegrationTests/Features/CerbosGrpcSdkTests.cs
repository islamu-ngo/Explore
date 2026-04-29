// ABOUTME: Cerbos gRPC SDK integration tests using the production Cerbos.Sdk client library.
// ABOUTME: Validates that the official gRPC client can connect to the containerized PDP and return correct decisions.

using Cerbos.Api.V1.Effect;
using Cerbos.Sdk;
using Cerbos.Sdk.Builder;
using Cerbos.Sdk.Response;
using Cerbos.Sdk.Utility;
using Event.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Grpc.Net.Client;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Production-faithful gRPC SDK tests mirroring how
/// <c>CerbosAuthorizationService</c> uses the Cerbos SDK.
/// Validates the gRPC transport path separately from HTTP API tests.
/// </summary>
[Category(TestCategories.PolicyContract)]
[ClassDataSource<SecurityInfrastructureFixture>(Shared = SharedType.PerAssembly)]
public class CerbosGrpcSdkTests : IDisposable
{
    private readonly ICerbosClient _client;

    public CerbosGrpcSdkTests(SecurityInfrastructureFixture infra)
    {
        var grpcEndpoint = infra.CerbosGrpcEndpoint;

        var builder = CerbosClientBuilder
            .ForTarget(grpcEndpoint)
            .WithGrpcChannelOptions(new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler(),
                DisposeHttpClient = true
            });

        if (grpcEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            builder = builder.WithPlaintext();
        }

        _client = builder.Build();
    }

    public void Dispose()
    {
        (_client as IDisposable)?.Dispose();
    }

    #region Instance Admin via gRPC SDK

    [Test]
    public async Task GrpcSdk_InstanceAdmin_ShouldBeAllowedAllEventActions()
    {
        var principal = Principal
            .NewInstance("user-instance-admin", "authenticated_user")
            .WithAttribute("isInstanceAdmin", AttributeValue.BoolValue(true))
            .WithAttribute("tenantMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>()))
            .WithAttribute("orgMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>()));

        var resource = ResourceEntry
            .NewInstance("event", "event-grpc-1")
            .WithActions("view", "create", "update", "delete");

        var response = await SendCheckResourcesAsync(principal, resource);
        var result = response.Find("event-grpc-1");

        result.Should().NotBeNull();
        AssertEffect(result!, "view", Effect.Allow);
        AssertEffect(result!, "create", Effect.Allow);
        AssertEffect(result!, "update", Effect.Allow);
        AssertEffect(result!, "delete", Effect.Allow);
    }

    #endregion

    #region Regular User via gRPC SDK

    [Test]
    public async Task GrpcSdk_RegularUser_ShouldOnlyViewEvents()
    {
        var principal = BuildRegularUserPrincipal();
        var resource = ResourceEntry
            .NewInstance("event", "event-grpc-2")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-1"))
            .WithAttribute("organizationId", AttributeValue.StringValue("org-1"))
            .WithActions("view", "create", "update", "delete");

        var response = await SendCheckResourcesAsync(principal, resource);
        var result = response.Find("event-grpc-2")!;

        AssertEffect(result, "view", Effect.Allow);
        AssertEffect(result, "create", Effect.Deny);
        AssertEffect(result, "update", Effect.Deny);
        AssertEffect(result, "delete", Effect.Deny);
    }

    #endregion

    #region Tenant Admin via gRPC SDK

    [Test]
    public async Task GrpcSdk_TenantAdmin_ShouldManageEventsInOwnTenant()
    {
        var principal = BuildTenantAdminPrincipal("tenant-1");
        var resource = ResourceEntry
            .NewInstance("event", "event-grpc-3")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-1"))
            .WithAttribute("organizationId", AttributeValue.StringValue("org-1"))
            .WithActions("view", "create", "update");

        var response = await SendCheckResourcesAsync(principal, resource);
        var result = response.Find("event-grpc-3")!;

        AssertEffect(result, "view", Effect.Allow);
        AssertEffect(result, "create", Effect.Allow);
        AssertEffect(result, "update", Effect.Allow);
    }

    [Test]
    public async Task GrpcSdk_TenantAdmin_ShouldBeDeniedInOtherTenant()
    {
        var principal = BuildTenantAdminPrincipal("tenant-1");
        var resource = ResourceEntry
            .NewInstance("event", "event-grpc-4")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-other"))
            .WithAttribute("organizationId", AttributeValue.StringValue("org-1"))
            .WithActions("create", "update", "delete");

        var response = await SendCheckResourcesAsync(principal, resource);
        var result = response.Find("event-grpc-4")!;

        AssertEffect(result, "create", Effect.Deny);
        AssertEffect(result, "update", Effect.Deny);
        AssertEffect(result, "delete", Effect.Deny);
    }

    #endregion

    #region Org Admin via gRPC SDK

    [Test]
    public async Task GrpcSdk_OrgAdmin_ShouldManageEventsInOwnOrg()
    {
        var principal = BuildOrgAdminPrincipal("org-1");
        var resource = ResourceEntry
            .NewInstance("event", "event-grpc-5")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-1"))
            .WithAttribute("organizationId", AttributeValue.StringValue("org-1"))
            .WithActions("view", "create", "update", "delete");

        var response = await SendCheckResourcesAsync(principal, resource);
        var result = response.Find("event-grpc-5")!;

        AssertEffect(result, "view", Effect.Allow);
        AssertEffect(result, "create", Effect.Allow);
        AssertEffect(result, "update", Effect.Allow);
        AssertEffect(result, "delete", Effect.Allow);
    }

    [Test]
    public async Task GrpcSdk_OrgAdmin_ShouldBeDeniedInOtherOrg()
    {
        var principal = BuildOrgAdminPrincipal("org-1");
        var resource = ResourceEntry
            .NewInstance("event", "event-grpc-6")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-1"))
            .WithAttribute("organizationId", AttributeValue.StringValue("org-other"))
            .WithActions("create", "update", "delete");

        var response = await SendCheckResourcesAsync(principal, resource);
        var result = response.Find("event-grpc-6")!;

        AssertEffect(result, "create", Effect.Deny);
        AssertEffect(result, "update", Effect.Deny);
        AssertEffect(result, "delete", Effect.Deny);
    }

    #endregion

    #region Batch Check via gRPC SDK

    [Test]
    public async Task GrpcSdk_BatchCheck_ShouldReturnResultsForMultipleResources()
    {
        var principal = BuildRegularUserPrincipal();

        var eventResource = ResourceEntry
            .NewInstance("event", "batch-event-1")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-1"))
            .WithAttribute("organizationId", AttributeValue.StringValue("org-1"))
            .WithActions("view");

        var orgResource = ResourceEntry
            .NewInstance("organization", "batch-org-1")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-1"))
            .WithAttribute("organizationId", AttributeValue.StringValue("org-1"))
            .WithActions("view");

        var request = CheckResourcesRequest
            .NewInstance()
            .WithRequestId(RequestId.Generate())
            .WithPrincipal(principal)
            .WithResourceEntries(eventResource, orgResource);

        var response = await _client.CheckResourcesAsync(request);

        var eventResult = response.Find("batch-event-1")!;
        var orgResult = response.Find("batch-org-1")!;

        AssertEffect(eventResult, "view", Effect.Allow);
        AssertEffect(orgResult, "view", Effect.Allow);
    }

    #endregion

    #region Tenant Setting Lock via gRPC SDK

    [Test]
    public async Task GrpcSdk_TenantAdmin_ShouldBeDeniedUpdateWhenLockedByInstance()
    {
        var principal = BuildTenantAdminPrincipal("tenant-1");
        var resource = ResourceEntry
            .NewInstance("tenant_setting", "setting-grpc-1")
            .WithAttribute("tenantId", AttributeValue.StringValue("tenant-1"))
            .WithAttribute("isLockedByInstance", AttributeValue.BoolValue(true))
            .WithActions("update");

        var response = await SendCheckResourcesAsync(principal, resource);
        var result = response.Find("setting-grpc-1")!;

        AssertEffect(result, "update", Effect.Deny,
            "tenant admin must be denied update when isLockedByInstance=true");
    }

    #endregion

    #region Helpers

    private async Task<CheckResourcesResponse> SendCheckResourcesAsync(
        Principal principal, ResourceEntry resource)
    {
        var request = CheckResourcesRequest
            .NewInstance()
            .WithRequestId(RequestId.Generate())
            .WithPrincipal(principal)
            .WithResourceEntries(resource);

        return await _client.CheckResourcesAsync(request);
    }

    private static void AssertEffect(
        CheckResourcesResponse.Types.ResultEntry result,
        string action,
        Effect expected,
        string? because = null)
    {
        result.Actions.TryGetValue(action, out var actual).Should().BeTrue(
            $"action '{action}' must be present in the Cerbos response");
        actual.Should().Be(expected, because ?? $"action '{action}' should be {expected}");
    }

    private static Principal BuildRegularUserPrincipal() =>
        Principal
            .NewInstance("user-regular-grpc", "authenticated_user")
            .WithAttribute("isInstanceAdmin", AttributeValue.BoolValue(false))
            .WithAttribute("tenantMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>()))
            .WithAttribute("orgMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>()));

    private static Principal BuildTenantAdminPrincipal(string tenantId) =>
        Principal
            .NewInstance("user-tenant-admin-grpc", "authenticated_user")
            .WithAttribute("isInstanceAdmin", AttributeValue.BoolValue(false))
            .WithAttribute("tenantMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>
            {
                [tenantId] = AttributeValue.StringValue("admin")
            }))
            .WithAttribute("orgMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>()));

    private static Principal BuildOrgAdminPrincipal(string orgId) =>
        Principal
            .NewInstance("user-org-admin-grpc", "authenticated_user")
            .WithAttribute("isInstanceAdmin", AttributeValue.BoolValue(false))
            .WithAttribute("tenantMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>()))
            .WithAttribute("orgMemberships", AttributeValue.MapValue(new Dictionary<string, AttributeValue>
            {
                [orgId] = AttributeValue.StringValue("admin")
            }));

    #endregion
}
