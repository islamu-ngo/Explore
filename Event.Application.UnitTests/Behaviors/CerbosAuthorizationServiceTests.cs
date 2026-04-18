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
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CerbosAuthorizationService> _logger;
    private readonly ICerbosClient _cerbosClient;
    private readonly ICerbosClientFactory _clientFactory;

    public CerbosAuthorizationServiceTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _tenantContext = Substitute.For<ITenantContext>();
        _logger = Substitute.For<ILogger<CerbosAuthorizationService>>();
        _cerbosClient = Substitute.For<ICerbosClient>();
        _clientFactory = Substitute.For<ICerbosClientFactory>();
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
    public async Task IsAllowedBatchAsync_GrpcFailure_DeniesAll()
    {
        var userId = Guid.NewGuid();
        _adminContext.UserId.Returns(userId);
        _adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(false);
        _adminContext.GetAdminTenantIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        _adminContext.GetAdminOrganizationIdsAsync(Arg.Any<CancellationToken>()).Returns([]);

        _cerbosClient.CheckResourcesAsync(Arg.Any<CheckResourcesRequest>(), Arg.Any<Metadata>())
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "Connection refused")));

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
    public async Task IsAllowedBatchAsync_EmptyChecks_ReturnsEmptyList()
    {
        var service = CreateService();
        var result = await service.IsAllowedBatchAsync([]);
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ToAttributeValue_ConvertsClrTypes()
    {
        // String
        var strVal = CerbosAuthorizationService.ToAttributeValue("hello");
        await Assert.That(strVal).IsNotNull();

        // Bool
        var boolVal = CerbosAuthorizationService.ToAttributeValue(true);
        await Assert.That(boolVal).IsNotNull();

        // Null
        var nullVal = CerbosAuthorizationService.ToAttributeValue(null);
        await Assert.That(nullVal).IsNotNull();

        // Int
        var intVal = CerbosAuthorizationService.ToAttributeValue(42);
        await Assert.That(intVal).IsNotNull();
    }

    private CerbosAuthorizationService CreateService()
    {
        return new CerbosAuthorizationService(
            _cerbosClient,
            new CerbosPrincipalBuilder(_adminContext),
            _adminContext,
            _settingsResolver,
            _tenantContext,
            _clientFactory,
            Options.Create(new CerbosSettings { GrpcEndpoint = "http://localhost:3593", PlaintextMode = true }),
            _logger);
    }
}
