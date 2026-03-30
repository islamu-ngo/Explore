using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Features.ExternalApiKeys.Handlers.Commands;
using Explore.Application.Features.ExternalApiKeys.Requests.Commands;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ExternalApiKeys.Commands;

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
                OwnerType = ExternalApiKeyOwnerType.User,
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
