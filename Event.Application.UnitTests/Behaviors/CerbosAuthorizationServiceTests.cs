// ABOUTME: Unit tests for CerbosAuthorizationService gRPC SDK request/response mapping and deny semantics.
// Verifies principal construction, missing-user fail-closed behavior, and gRPC error handling.

using Cerbos.Api.V1.Effect;
using Cerbos.Sdk;
using Cerbos.Sdk.Builder;
using Cerbos.Sdk.Response;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Settings;
using Explore.Infrastructure.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Event.Application.UnitTests.Behaviors;

public class CerbosAuthorizationServiceTests
{
    private readonly IAdminContext _adminContext;
    private readonly IMachinePrincipalAccessor _machinePrincipalAccessor;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CerbosAuthorizationService> _logger;
    private readonly ICerbosClient _cerbosClient;
    private readonly ICerbosClientFactory _clientFactory;

    public CerbosAuthorizationServiceTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _machinePrincipalAccessor = Substitute.For<IMachinePrincipalAccessor>();
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _logger = Substitute.For<ILogger<CerbosAuthorizationService>>();
        _cerbosClient = Substitute.For<ICerbosClient>();
        _clientFactory = Substitute.For<ICerbosClientFactory>();

        _machinePrincipalAccessor.IsMachineCaller.Returns(false);
        _machinePrincipalAccessor.Current.Returns((Explore.Application.Authentication.ApiKeyPrincipalContext?)null);
    }

    [Test]
    public async Task IsAllowedBatchAsync_NoUserId_DeniesAllWithoutGrpcCall()
    {
        _adminContext.UserId.Returns((Guid?)null);

        var service = CreateService();
        var checks = new List<AuthorizationCheck>
        {
            new("organization", "org-1", "update", null),
            new("tenant_setting", "setting-key", "read", null)
        };

        var result = await service.IsAllowedBatchAsync(checks);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsFalse();
        await Assert.That(result[1]).IsFalse();
        await _cerbosClient.DidNotReceive().CheckResourcesAsync(
            Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_MapsAllowAndDenyFromCerbosResponse()
    {
        var userId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var protoResponse = new Cerbos.Api.V1.Response.CheckResourcesResponse();
        protoResponse.Results.Add(CreateResultEntry("org-1", "organization", "update", Effect.Allow));
        protoResponse.Results.Add(CreateResultEntry("org-2", "organization", "delete", Effect.Deny));

        _cerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns(new CheckResourcesResponse(protoResponse));

        var service = CreateService();
        var checks = new List<AuthorizationCheck>
        {
            new("organization", "org-1", "update", null),
            new("organization", "org-2", "delete", null)
        };

        var result = await service.IsAllowedBatchAsync(checks);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsTrue();
        await Assert.That(result[1]).IsFalse();
        await _cerbosClient.Received(1)
            .CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>());
    }

    [Test]
    public async Task IsAllowedBatchAsync_GrpcFailure_DeniesAll()
    {
        var userId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);

        _cerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .ThrowsAsync(new RpcException(new Status(StatusCode.Unavailable, "PDP unreachable")));

        var service = CreateService();
        var checks = new List<AuthorizationCheck>
        {
            new("organization", "org-1", "update", null),
            new("tenant_setting", "setting-1", "update", null)
        };

        var result = await service.IsAllowedBatchAsync(checks);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsFalse();
        await Assert.That(result[1]).IsFalse();
    }

    [Test]
    public async Task CheckSettingAccessAsync_TenantScope_SendsLockAndTenantAttributes()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([tenantId]);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _settingsResolver.ResolveWithMetadataAsync("events.require_approval", Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new ResolvedSetting { Key = "events.require_approval", IsLocked = true });

        Cerbos.Api.V1.Request.CheckResourcesRequest? capturedRequest = null;

        var protoResponse = new Cerbos.Api.V1.Response.CheckResourcesResponse();
        protoResponse.Results.Add(CreateResultEntry("events.require_approval", "tenant_setting", "update", Effect.Deny));

        _cerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Returns(call =>
            {
                capturedRequest = call.ArgAt<CheckResourcesRequest>(0).ToCheckResourcesRequest();
                return new CheckResourcesResponse(protoResponse);
            });

        var service = CreateService();
        var allowed = await service.CheckSettingAccessAsync("events.require_approval", "update", tenantId: tenantId);

        await Assert.That(allowed).IsFalse();
        await Assert.That(capturedRequest).IsNotNull();

        var resource = capturedRequest!.Resources[0];
        await Assert.That(resource.Resource.Kind).IsEqualTo("tenant_setting");
        await Assert.That(resource.Resource.Id).IsEqualTo("events.require_approval");

        var attrs = resource.Resource.Attr;
        await Assert.That(attrs.ContainsKey("tenantId")).IsTrue();
        await Assert.That(attrs["tenantId"].StringValue).IsEqualTo(tenantId.ToString());
        await Assert.That(attrs.ContainsKey("isLockedByInstance")).IsTrue();
        await Assert.That(attrs["isLockedByInstance"].BoolValue).IsTrue();
    }

    [Test]
    public async Task IsAllowedBatchAsync_EmptyChecks_ReturnsEmptyList()
    {
        var service = CreateService();
        var result = await service.IsAllowedBatchAsync([]);
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ToAttributeValue_ConvertsClrTypes()
    {
        var strVal = CerbosAuthorizationService.ToAttributeValue("hello");
        await Assert.That(strVal).IsNotNull();

        var boolVal = CerbosAuthorizationService.ToAttributeValue(true);
        await Assert.That(boolVal).IsNotNull();

        var nullVal = CerbosAuthorizationService.ToAttributeValue(null);
        await Assert.That(nullVal).IsNotNull();

        var intVal = CerbosAuthorizationService.ToAttributeValue(42);
        await Assert.That(intVal).IsNotNull();
    }

    private CerbosAuthorizationService CreateService()
    {
        return new CerbosAuthorizationService(
            _cerbosClient,
            new CerbosPrincipalBuilder(_adminContext, _machinePrincipalAccessor),
            _adminContext,
            _machinePrincipalAccessor,
            _settingsResolver,
            _tenantContext,
            _clientFactory,
            Options.Create(new CerbosSettings { GrpcEndpoint = "http://localhost:3593", PlaintextMode = true }),
            _logger);
    }

    private static Cerbos.Api.V1.Response.CheckResourcesResponse.Types.ResultEntry CreateResultEntry(
        string resourceId, string resourceKind, string action, Effect effect)
    {
        var entry = new Cerbos.Api.V1.Response.CheckResourcesResponse.Types.ResultEntry
        {
            Resource = new Cerbos.Api.V1.Response.CheckResourcesResponse.Types.ResultEntry.Types.Resource
            {
                Id = resourceId,
                Kind = resourceKind
            }
        };
        entry.Actions.Add(action, effect);
        return entry;
    }
}
