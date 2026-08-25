// ABOUTME: Unit tests for the event-session custom-property projection rebuild command handler.
// ABOUTME: Mirrors event projection quota boundary coverage for session-scope rebuilds.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Handlers.Commands;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain.Settings.Definitions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionCustomPropertyProjections.Commands;

public class RebuildEventSessionCustomPropertyProjectionCommandHandlerTests
{
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly RebuildEventSessionCustomPropertyProjectionCommandHandler _handler;

    public RebuildEventSessionCustomPropertyProjectionCommandHandlerTests()
    {
        _projectionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        _quotaResolver
            .GetIntAsync(
                CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(500);
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test-session"));
        _handler = new RebuildEventSessionCustomPropertyProjectionCommandHandler(
            _projectionUpdater,
            _quotaResolver,
            new ProjectionMetrics(meterFactory));
    }

    [Test]
    public async Task Handle_WhenBatchSizeExceedsQuota_ReturnsQuotaFailure()
    {
        var tenantId = Guid.NewGuid();
        _quotaResolver
            .GetIntAsync(
                CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key,
                tenantId,
                Arg.Any<CancellationToken>())
            .Returns(25);

        var command = new RebuildEventSessionCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = tenantId, BatchSize = 26 }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(FailureCodes.QuotaExceeded);
        await Assert.That(result.QuotaExceeded).IsNotNull();
        await Assert.That(result.QuotaExceeded!.QuotaKey).IsEqualTo(CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key);
        await Assert.That(result.QuotaExceeded.Limit).IsEqualTo(25);
        await Assert.That(result.QuotaExceeded.Actual).IsNull();
        await Assert.That(result.QuotaExceeded.Attempted).IsEqualTo(26);
        await Assert.That(result.QuotaExceeded.Scope).IsEqualTo("event_session_custom_property_projection_rebuild");
        await Assert.That(result.QuotaExceeded.TenantId).IsEqualTo(tenantId);
        await _projectionUpdater.DidNotReceive()
            .RebuildForTenantAsync(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithBatchSizeOneBelowQuota_PassesBatchSizeToUpdater()
    {
        var tenantId = Guid.NewGuid();
        _quotaResolver
            .GetIntAsync(
                CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key,
                tenantId,
                Arg.Any<CancellationToken>())
            .Returns(25);
        _projectionUpdater
            .RebuildForTenantAsync(tenantId, 24, Arg.Any<CancellationToken>())
            .Returns(new ProjectionRebuildResult(true, 10, 0, 0));

        var command = new RebuildEventSessionCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = tenantId, BatchSize = 24 }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.QuotaExceeded).IsNull();
        await _projectionUpdater.Received(1)
            .RebuildForTenantAsync(tenantId, 24, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithBatchSizeEqualToQuota_PassesBatchSizeToUpdater()
    {
        var tenantId = Guid.NewGuid();
        _quotaResolver
            .GetIntAsync(
                CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key,
                tenantId,
                Arg.Any<CancellationToken>())
            .Returns(25);
        _projectionUpdater
            .RebuildForTenantAsync(tenantId, 25, Arg.Any<CancellationToken>())
            .Returns(new ProjectionRebuildResult(true, 10, 0, 0));

        var command = new RebuildEventSessionCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = tenantId, BatchSize = 25 }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.QuotaExceeded).IsNull();
        await _projectionUpdater.Received(1)
            .RebuildForTenantAsync(tenantId, 25, Arg.Any<CancellationToken>());
    }
}
