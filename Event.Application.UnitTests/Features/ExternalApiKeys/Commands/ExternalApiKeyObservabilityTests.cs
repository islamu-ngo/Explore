// ABOUTME: Unit tests for external API-key command observability and scope validation.
// ABOUTME: Protects metrics, MCP scope catalog checks, and immutable owner policy behavior.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Exceptions;
using Explore.Application.Features.ExternalApiKeys.Handlers.Commands;
using Explore.Application.Features.ExternalApiKeys.Requests.Commands;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ExternalApiKeys.Commands;

[NotInParallel("BusinessMetricsMeter")]
public class ExternalApiKeyObservabilityTests
{
    [Test]
    public async Task CreateExternalApiKeyCommandHandler_WithSuccessfulRequest_RecordsCreationMetric()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var groupRepository = Substitute.For<IGroupRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var tenantContext = Substitute.For<ITenantContext>();
        var logger = Substitute.For<ILogger<CreateExternalApiKeyCommandHandler>>();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        userContext.GetRequiredUserId().Returns(userId);
        tenantContext.TenantId.Returns(tenantId);
        externalApiKeyRepository.ExistsByOwnerAndName(ExternalApiKeyOwnerType.User, userId, "Deploy Bot")
            .Returns(false);
        externalApiKeyRepository.Create(Arg.Any<ExternalApiKey>())
            .Returns(call =>
            {
                var entity = call.Arg<ExternalApiKey>();
                entity.Id = Guid.NewGuid();
                return entity;
            });

        var handler = new CreateExternalApiKeyCommandHandler(
            externalApiKeyRepository,
            organizationRepository,
            organizationMemberRepository,
            groupMemberRepository,
            groupRepository,
            adminContext,
            userContext,
            tenantContext,
            metrics,
            logger);

        var command = new CreateExternalApiKeyCommand
        {
            ExternalApiKeyDto = new CreateExternalApiKeyDto
            {
                Name = "Deploy Bot",
                ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.User,
                Scopes = ["events:read", "events:write"]
            }
        };

        var response = await handler.Handle(command, CancellationToken.None);

        await Assert.That(response.Success).IsTrue();

        var measurement = await metricsCapture.SingleAsync("explore.external_api_keys.created");
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo(tenantId.ToString());
        await Assert.That(measurement.Tags["owner_type"]?.ToString()).IsEqualTo(ExternalApiKeyOwnerType.User.ToString());
    }

    [Test]
    public async Task CreateExternalApiKeyCommandHandler_WithUnknownMcpScope_ReturnsValidationError()
    {
        var metrics = CreateMetrics();

        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var groupRepository = Substitute.For<IGroupRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var tenantContext = Substitute.For<ITenantContext>();
        var logger = Substitute.For<ILogger<CreateExternalApiKeyCommandHandler>>();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        userContext.GetRequiredUserId().Returns(userId);
        tenantContext.TenantId.Returns(tenantId);
        externalApiKeyRepository.ExistsByOwnerAndName(ExternalApiKeyOwnerType.User, userId, "MCP Bot")
            .Returns(false);

        var handler = new CreateExternalApiKeyCommandHandler(
            externalApiKeyRepository,
            organizationRepository,
            organizationMemberRepository,
            groupMemberRepository,
            groupRepository,
            adminContext,
            userContext,
            tenantContext,
            metrics,
            logger);

        var response = await handler.Handle(
            new CreateExternalApiKeyCommand
            {
                ExternalApiKeyDto = new CreateExternalApiKeyDto
                {
                    Name = "MCP Bot",
                    ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.User,
                    Scopes = [ExternalApiKeyScopes.McpRead, "mcp:teleport"]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error => error.Contains("Invalid scopes", StringComparison.OrdinalIgnoreCase));
        await Assert.That(response.Errors).Contains(error => error.Contains("mcp:teleport", StringComparison.OrdinalIgnoreCase));
        await externalApiKeyRepository.DidNotReceive().Create(Arg.Any<ExternalApiKey>());
    }

    [Test]
    public async Task CreateExternalApiKeyCommandHandler_WithTenantOwnerAndNoAdminAuthority_ThrowsAuthorizationException()
    {
        var metrics = CreateMetrics();

        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var groupRepository = Substitute.For<IGroupRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var tenantContext = Substitute.For<ITenantContext>();
        var logger = Substitute.For<ILogger<CreateExternalApiKeyCommandHandler>>();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        userContext.GetRequiredUserId().Returns(userId);
        tenantContext.TenantId.Returns(tenantId);
        externalApiKeyRepository.ExistsByOwnerAndName(
                ExternalApiKeyOwnerType.Tenant,
                tenantId,
                "Tenant Bot",
                Arg.Any<CancellationToken>())
            .Returns(false);
        adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateExternalApiKeyCommandHandler(
            externalApiKeyRepository,
            organizationRepository,
            organizationMemberRepository,
            groupMemberRepository,
            groupRepository,
            adminContext,
            userContext,
            tenantContext,
            metrics,
            logger);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            handler.Handle(
                new CreateExternalApiKeyCommand
                {
                    ExternalApiKeyDto = new CreateExternalApiKeyDto
                    {
                        Name = "Tenant Bot",
                        ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.Tenant,
                        Scopes = [ExternalApiKeyScopes.AdminTenant]
                    }
                },
                CancellationToken.None));

        await externalApiKeyRepository.DidNotReceive().Create(Arg.Any<ExternalApiKey>());
    }

    [Test]
    public async Task UpdateExternalApiKeyPolicyCommandHandler_WithUnknownMcpScope_ReturnsValidationError()
    {
        var metrics = CreateMetrics();

        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var logger = Substitute.For<ILogger<UpdateExternalApiKeyPolicyCommandHandler>>();

        var userId = Guid.NewGuid();
        var externalApiKey = new ExternalApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            Name = "MCP Bot",
            KeyId = "key-mcp-validation",
            SecretHash = "hash",
            Scopes = ExternalApiKeyScopes.McpRead,
            OwnerType = ExternalApiKeyOwnerType.User,
            OwnerId = userId,
            ExternalApiKeyStatusId = (int)ExternalApiKeyStatusEnum.Active,
            ExternalApiKeyStatus = null!,
            ExternalApiKeyCreditPeriodId = (int)ExternalApiKeyCreditPeriodEnum.None,
            ExternalApiKeyCreditPeriod = null!
        };

        userContext.GetRequiredUserId().Returns(userId);
        externalApiKeyRepository.GetByIdIgnoringTenantFilter(externalApiKey.Id).Returns(externalApiKey);

        var handler = new UpdateExternalApiKeyPolicyCommandHandler(
            externalApiKeyRepository,
            organizationMemberRepository,
            groupMemberRepository,
            adminContext,
            userContext,
            metrics,
            logger);

        var response = await handler.Handle(
            new UpdateExternalApiKeyPolicyCommand
            {
                ExternalApiKeyPolicyDto = new UpdateExternalApiKeyPolicyDto
                {
                    Id = externalApiKey.Id,
                    Name = "MCP Bot",
                    Scopes = [ExternalApiKeyScopes.McpRead, "mcp:teleport"]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error => error.Contains("Invalid scopes", StringComparison.OrdinalIgnoreCase));
        await Assert.That(response.Errors).Contains(error => error.Contains("mcp:teleport", StringComparison.OrdinalIgnoreCase));
        await externalApiKeyRepository.DidNotReceive().Update(Arg.Any<ExternalApiKey>());
    }

    [Test]
    public async Task UpdateExternalApiKeyPolicyCommandHandler_WithInstanceAdminKeyNameConflict_UsesTenantFilterBypass()
    {
        var metrics = CreateMetrics();

        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var logger = Substitute.For<ILogger<UpdateExternalApiKeyPolicyCommandHandler>>();

        var userId = Guid.NewGuid();
        var externalApiKey = new ExternalApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            Tenant = null!,
            Name = "Original Platform Bot",
            KeyId = "key-platform-validation",
            SecretHash = "hash",
            Scopes = ExternalApiKeyScopes.AdminInstance,
            OwnerType = ExternalApiKeyOwnerType.InstanceAdmin,
            OwnerId = userId,
            ExternalApiKeyStatusId = (int)ExternalApiKeyStatusEnum.Active,
            ExternalApiKeyStatus = null!,
            ExternalApiKeyCreditPeriodId = (int)ExternalApiKeyCreditPeriodEnum.None,
            ExternalApiKeyCreditPeriod = null!
        };

        userContext.GetRequiredUserId().Returns(userId);
        adminContext.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        externalApiKeyRepository.GetByIdIgnoringTenantFilter(externalApiKey.Id, Arg.Any<CancellationToken>())
            .Returns(externalApiKey);
        externalApiKeyRepository.ExistsByOwnerAndNameIgnoringTenantFilter(
                ExternalApiKeyOwnerType.InstanceAdmin,
                userId,
                "Platform Ops",
                Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new UpdateExternalApiKeyPolicyCommandHandler(
            externalApiKeyRepository,
            organizationMemberRepository,
            groupMemberRepository,
            adminContext,
            userContext,
            metrics,
            logger);

        var response = await handler.Handle(
            new UpdateExternalApiKeyPolicyCommand
            {
                ExternalApiKeyPolicyDto = new UpdateExternalApiKeyPolicyDto
                {
                    Id = externalApiKey.Id,
                    Name = "Platform Ops",
                    Scopes = [ExternalApiKeyScopes.AdminInstance]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error => error.Contains("same name", StringComparison.OrdinalIgnoreCase));
        await externalApiKeyRepository.Received(1).ExistsByOwnerAndNameIgnoringTenantFilter(
            ExternalApiKeyOwnerType.InstanceAdmin,
            userId,
            "Platform Ops",
            Arg.Any<CancellationToken>());
        await externalApiKeyRepository.DidNotReceive().ExistsByOwnerAndName(
            Arg.Any<ExternalApiKeyOwnerType>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await externalApiKeyRepository.DidNotReceive().Update(Arg.Any<ExternalApiKey>());
    }

    [Test]
    public async Task BusinessMetrics_RecordExternalApiKeyAuthentication_UsesBoundedNonSecretTags()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        metrics.RecordExternalApiKeyAuthentication("invalid", tenantId: "unknown", ownerType: "unknown");

        var measurement = await metricsCapture.SingleAsync("explore.external_api_keys.authentication_attempts");
        await Assert.That(measurement.Tags["outcome"]?.ToString()).IsEqualTo("invalid");
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo("unknown");
        await Assert.That(measurement.Tags["owner_type"]?.ToString()).IsEqualTo("unknown");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("api_key");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("secret");
        await Assert.That(measurement.Tags.Keys).DoesNotContain("path");
    }

    [Test]
    public async Task RevokeExternalApiKeyCommandHandler_WithSuccessfulRequest_RecordsRevocationMetric()
    {
        using var metricsCapture = new MetricsCapture();
        var metrics = CreateMetrics();

        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var logger = Substitute.For<ILogger<RevokeExternalApiKeyCommandHandler>>();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var externalApiKey = new ExternalApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Tenant = null!,
            Name = "Ops Bot",
            KeyId = "key-1234567890",
            SecretHash = "hash",
            Scopes = "events:read",
            OwnerType = ExternalApiKeyOwnerType.User,
            OwnerId = userId,
            ExternalApiKeyStatusId = (int)ExternalApiKeyStatusEnum.Active,
            ExternalApiKeyStatus = null!,
            ExternalApiKeyCreditPeriodId = (int)ExternalApiKeyCreditPeriodEnum.None,
            ExternalApiKeyCreditPeriod = null!
        };

        userContext.GetRequiredUserId().Returns(userId);
        externalApiKeyRepository.GetByIdIgnoringTenantFilter(externalApiKey.Id).Returns(externalApiKey);

        var handler = new RevokeExternalApiKeyCommandHandler(
            externalApiKeyRepository,
            organizationMemberRepository,
            groupMemberRepository,
            adminContext,
            userContext,
            metrics,
            logger);

        var result = await handler.Handle(new RevokeExternalApiKeyCommand { Id = externalApiKey.Id }, CancellationToken.None);

        await Assert.That(result).IsTrue();

        var measurement = await metricsCapture.SingleAsync("explore.external_api_keys.revoked");
        await Assert.That(measurement.Tags["tenant_id"]?.ToString()).IsEqualTo(tenantId.ToString());
        await Assert.That(measurement.Tags["owner_type"]?.ToString()).IsEqualTo(ExternalApiKeyOwnerType.User.ToString());
    }

    private static BusinessMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));
        return new BusinessMetrics(meterFactory);
    }

    private sealed class MetricsCapture : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly List<Measurement> _measurements = [];

        public MetricsCapture()
        {
            _listener = new MeterListener();
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BusinessMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                _measurements.Add(new Measurement(
                    instrument.Name,
                    measurement,
                    tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value)));
            });

            _listener.Start();
        }

        public Task<Measurement> SingleAsync(string instrumentName)
        {
            var matches = _measurements.Where(measurement => measurement.InstrumentName == instrumentName).ToList();
            return Task.FromResult(matches.Single());
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed record Measurement(string InstrumentName, long Value, IReadOnlyDictionary<string, object?> Tags);
}
