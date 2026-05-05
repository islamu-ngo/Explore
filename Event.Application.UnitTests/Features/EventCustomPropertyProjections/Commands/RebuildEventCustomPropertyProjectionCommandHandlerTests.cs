// ABOUTME: Unit tests for the event custom-property projection rebuild command handler.
// ABOUTME: Validates validation, lock-acquired/skipped scenarios, and response DTO population.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.EventCustomPropertyProjections.Handlers.Commands;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;
using Explore.Application.Telemetry;
using Explore.Domain.Settings.Definitions;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventCustomPropertyProjections.Commands;

public class RebuildEventCustomPropertyProjectionCommandHandlerTests
{
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;
    private readonly ProjectionMetrics _metrics;
    private readonly RebuildEventCustomPropertyProjectionCommandHandler _handler;

    public RebuildEventCustomPropertyProjectionCommandHandlerTests()
    {
        _projectionUpdater = Substitute.For<IEventCustomPropertyProjectionUpdater>();
        _quotaResolver = Substitute.For<ICustomPropertyQuotaResolver>();
        _quotaResolver
            .GetIntAsync(
                CustomPropertyQuotaSettingDefinitions.ProjectionRebuildBatchSize.Key,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(500);
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));
        _metrics = new ProjectionMetrics(meterFactory);
        _handler = new RebuildEventCustomPropertyProjectionCommandHandler(_projectionUpdater, _quotaResolver, _metrics);
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccess()
    {
        var tenantId = Guid.NewGuid();
        _projectionUpdater
            .RebuildForTenantAsync(tenantId, null, Arg.Any<CancellationToken>())
            .Returns(new ProjectionRebuildResult(true, 150, 0, 3));

        var command = new RebuildEventCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = tenantId }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.LockAcquired).IsTrue();
        await Assert.That(result.Id.RowsProcessed).IsEqualTo(150);
        await Assert.That(result.Id.RowsFailed).IsEqualTo(0);
        await Assert.That(result.Id.DrainedDirtyScopes).IsEqualTo(3);
    }

    [Test]
    public async Task Handle_WhenLockNotAcquired_ReturnsSuccessWithSkipMessage()
    {
        var tenantId = Guid.NewGuid();
        _projectionUpdater
            .RebuildForTenantAsync(tenantId, null, Arg.Any<CancellationToken>())
            .Returns(new ProjectionRebuildResult(false, 0, 0, 0));

        var command = new RebuildEventCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = tenantId }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.LockAcquired).IsFalse();
        await Assert.That(result.Message!).Contains("skipped");
    }

    [Test]
    public async Task Handle_WithEmptyTenantId_ReturnsValidationError()
    {
        var command = new RebuildEventCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = Guid.Empty }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Handle_WithNegativeBatchSize_ReturnsValidationError()
    {
        var command = new RebuildEventCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = Guid.NewGuid(), BatchSize = -1 }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
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

        var command = new RebuildEventCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = tenantId, BatchSize = 26 }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!).Contains(e => e.Contains("quota_exceeded", StringComparison.Ordinal));
        await _projectionUpdater.DidNotReceive()
            .RebuildForTenantAsync(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithAllowedBatchSize_PassesBatchSizeToUpdater()
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

        var command = new RebuildEventCustomPropertyProjectionCommand
        {
            RequestDto = new RebuildProjectionRequestDto { TenantId = tenantId, BatchSize = 25 }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _projectionUpdater.Received(1)
            .RebuildForTenantAsync(tenantId, 25, Arg.Any<CancellationToken>());
    }
}
