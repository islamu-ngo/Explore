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
using Explore.Application.Services;
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
    public async Task CreateExternalApiKeyCommandHandler_WithSuccessfulRequest_DoesNotLogOrPersistRawApiKey()
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
        var logger = new CapturingLogger<CreateExternalApiKeyCommandHandler>();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        ExternalApiKey? persisted = null;

        userContext.GetRequiredUserId().Returns(userId);
        tenantContext.TenantId.Returns(tenantId);
        externalApiKeyRepository.ExistsByOwnerAndName(
                ExternalApiKeyOwnerType.User,
                userId,
                "Deploy Bot",
                Arg.Any<CancellationToken>())
            .Returns(false);
        externalApiKeyRepository.Create(Arg.Any<ExternalApiKey>())
            .Returns(call =>
            {
                persisted = call.Arg<ExternalApiKey>();
                persisted.Id = Guid.NewGuid();
                return persisted;
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

        var response = await handler.Handle(
            new CreateExternalApiKeyCommand
            {
                ExternalApiKeyDto = new CreateExternalApiKeyDto
                {
                    Name = "Deploy Bot",
                    ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.User,
                    Scopes = ["events:read", "events:write"]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.ApiKey).IsNotNull();
        await Assert.That(persisted).IsNotNull();
        await Assert.That(ApiKeyHashing.TryParsePersistedApiKey(response.ApiKey!, out var keyId, out var secret)).IsTrue();
        await Assert.That(persisted!.KeyId).IsEqualTo(keyId);
        await Assert.That(persisted.SecretHash).IsEqualTo(ApiKeyHashing.ComputeHash(secret));
        await Assert.That(persisted.SecretHash).IsNotEqualTo(response.ApiKey);

        var renderedLogs = string.Join('\n', logger.Messages);

        await Assert.That(logger.Messages.Count).IsGreaterThan(0);
        await Assert.That(renderedLogs).DoesNotContain(response.ApiKey!);
        await Assert.That(renderedLogs).DoesNotContain(secret);
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
    public async Task CreateExternalApiKeyCommandHandler_WithNameControlCharacter_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var fixture = CreateCreateHandlerFixture(userId, Guid.NewGuid());

        var response = await fixture.Handler.Handle(
            new CreateExternalApiKeyCommand
            {
                ExternalApiKeyDto = new CreateExternalApiKeyDto
                {
                    Name = "Ops\nBot",
                    ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.User,
                    Scopes = [ExternalApiKeyScopes.EventsRead]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error => error == "API key name must not contain control characters.");
        await fixture.ExternalApiKeyRepository.DidNotReceive().Create(Arg.Any<ExternalApiKey>());
    }

    [Test]
    public async Task CreateExternalApiKeyCommandHandler_WithDescriptionTooLong_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var fixture = CreateCreateHandlerFixture(userId, Guid.NewGuid());

        var response = await fixture.Handler.Handle(
            new CreateExternalApiKeyCommand
            {
                ExternalApiKeyDto = new CreateExternalApiKeyDto
                {
                    Name = "Ops Bot",
                    Description = new string('a', 1001),
                    ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.User,
                    Scopes = [ExternalApiKeyScopes.EventsRead]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error => error == "API key description cannot exceed 1000 characters.");
        await fixture.ExternalApiKeyRepository.DidNotReceive().Create(Arg.Any<ExternalApiKey>());
    }

    [Test]
    public async Task CreateExternalApiKeyCommandHandler_WithPaddedDuplicateName_ChecksNormalizedName()
    {
        var userId = Guid.NewGuid();
        var fixture = CreateCreateHandlerFixture(userId, Guid.NewGuid());
        fixture.ExternalApiKeyRepository.ExistsByOwnerAndName(
                ExternalApiKeyOwnerType.User,
                userId,
                "Ops Bot",
                Arg.Any<CancellationToken>())
            .Returns(true);

        var response = await fixture.Handler.Handle(
            new CreateExternalApiKeyCommand
            {
                ExternalApiKeyDto = new CreateExternalApiKeyDto
                {
                    Name = " Ops Bot ",
                    ExternalApiKeyOwnerTypeId = (int)ExternalApiKeyOwnerType.User,
                    Scopes = [ExternalApiKeyScopes.EventsRead]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error => error == "An API key with the same name already exists for this owner.");
        await fixture.ExternalApiKeyRepository.Received(1).ExistsByOwnerAndName(
            ExternalApiKeyOwnerType.User,
            userId,
            "Ops Bot",
            Arg.Any<CancellationToken>());
        await fixture.ExternalApiKeyRepository.DidNotReceive().Create(Arg.Any<ExternalApiKey>());
    }

    [Test]
    public async Task UpdateExternalApiKeyPolicyCommandHandler_WithNameControlCharacter_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var externalApiKey = new ExternalApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            Name = "Ops Bot",
            KeyId = "key-policy-validation",
            SecretHash = "hash",
            Scopes = ExternalApiKeyScopes.EventsRead,
            OwnerType = ExternalApiKeyOwnerType.User,
            OwnerId = userId,
            ExternalApiKeyStatusId = (int)ExternalApiKeyStatusEnum.Active,
            ExternalApiKeyStatus = null!,
            ExternalApiKeyCreditPeriodId = (int)ExternalApiKeyCreditPeriodEnum.None,
            ExternalApiKeyCreditPeriod = null!
        };
        var fixture = CreateUpdateHandlerFixture(userId, externalApiKey);

        var response = await fixture.Handler.Handle(
            new UpdateExternalApiKeyPolicyCommand
            {
                ExternalApiKeyPolicyDto = new UpdateExternalApiKeyPolicyDto
                {
                    Id = externalApiKey.Id,
                    Name = "Ops\nBot",
                    Scopes = [ExternalApiKeyScopes.EventsWrite]
                }
            },
            CancellationToken.None);

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.Errors).Contains(error => error == "API key name must not contain control characters.");
        await fixture.ExternalApiKeyRepository.DidNotReceive().Update(Arg.Any<ExternalApiKey>());
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

    private static CreateHandlerFixture CreateCreateHandlerFixture(Guid userId, Guid tenantId)
    {
        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationRepository = Substitute.For<IOrganizationRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var groupRepository = Substitute.For<IGroupRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var tenantContext = Substitute.For<ITenantContext>();
        var logger = Substitute.For<ILogger<CreateExternalApiKeyCommandHandler>>();

        userContext.GetRequiredUserId().Returns(userId);
        tenantContext.TenantId.Returns(tenantId);

        return new CreateHandlerFixture(
            externalApiKeyRepository,
            new CreateExternalApiKeyCommandHandler(
                externalApiKeyRepository,
                organizationRepository,
                organizationMemberRepository,
                groupMemberRepository,
                groupRepository,
                adminContext,
                userContext,
                tenantContext,
                CreateMetrics(),
                logger));
    }

    private static UpdateHandlerFixture CreateUpdateHandlerFixture(Guid userId, ExternalApiKey externalApiKey)
    {
        var externalApiKeyRepository = Substitute.For<IExternalApiKeyRepository>();
        var organizationMemberRepository = Substitute.For<IOrganizationMemberRepository>();
        var groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        var adminContext = Substitute.For<IAdminContext>();
        var userContext = Substitute.For<IUserContext>();
        var logger = Substitute.For<ILogger<UpdateExternalApiKeyPolicyCommandHandler>>();

        userContext.GetRequiredUserId().Returns(userId);
        externalApiKeyRepository.GetByIdIgnoringTenantFilter(externalApiKey.Id, Arg.Any<CancellationToken>())
            .Returns(externalApiKey);

        return new UpdateHandlerFixture(
            externalApiKeyRepository,
            new UpdateExternalApiKeyPolicyCommandHandler(
                externalApiKeyRepository,
                organizationMemberRepository,
                groupMemberRepository,
                adminContext,
                userContext,
                CreateMetrics(),
                logger));
    }

    private sealed record CreateHandlerFixture(
        IExternalApiKeyRepository ExternalApiKeyRepository,
        CreateExternalApiKeyCommandHandler Handler);

    private sealed record UpdateHandlerFixture(
        IExternalApiKeyRepository ExternalApiKeyRepository,
        UpdateExternalApiKeyPolicyCommandHandler Handler);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
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
