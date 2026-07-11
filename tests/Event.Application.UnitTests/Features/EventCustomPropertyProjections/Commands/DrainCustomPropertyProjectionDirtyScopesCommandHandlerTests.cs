// ABOUTME: Unit tests for the dirty-scope drain command handler.
// ABOUTME: Validates routing to correct projection updater by name, validation, and unknown projection rejection.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.EventCustomPropertyProjections.Handlers.Commands;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;
using Explore.Application.Telemetry;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventCustomPropertyProjections.Commands;

public class DrainCustomPropertyProjectionDirtyScopesCommandHandlerTests
{
    private readonly IEventCustomPropertyProjectionUpdater _eventUpdater;
    private readonly IEventSessionCustomPropertyProjectionUpdater _sessionUpdater;
    private readonly ProjectionMetrics _metrics;
    private readonly DrainCustomPropertyProjectionDirtyScopesCommandHandler _handler;

    public DrainCustomPropertyProjectionDirtyScopesCommandHandlerTests()
    {
        _eventUpdater = Substitute.For<IEventCustomPropertyProjectionUpdater>();
        _sessionUpdater = Substitute.For<IEventSessionCustomPropertyProjectionUpdater>();
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));
        _metrics = new ProjectionMetrics(meterFactory);
        _handler = new DrainCustomPropertyProjectionDirtyScopesCommandHandler(_eventUpdater, _sessionUpdater, _metrics);
    }

    [Test]
    public async Task Handle_WithEventProjectionName_DrainsEventScopes()
    {
        var tenantId = Guid.NewGuid();
        _eventUpdater
            .DrainDirtyScopesForTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(5);

        var command = new DrainCustomPropertyProjectionDirtyScopesCommand
        {
            RequestDto = new DrainDirtyScopesRequestDto
            {
                TenantId = tenantId,
                ProjectionName = IEventCustomPropertyProjectionUpdater.ProjectionName
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsNotNull();
        await Assert.That(result.Id!.DrainedCount).IsEqualTo(5);
        await _eventUpdater.Received(1).DrainDirtyScopesForTenantAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithSessionProjectionName_DrainsSessionScopes()
    {
        var tenantId = Guid.NewGuid();
        _sessionUpdater
            .DrainDirtyScopesForTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(2);

        var command = new DrainCustomPropertyProjectionDirtyScopesCommand
        {
            RequestDto = new DrainDirtyScopesRequestDto
            {
                TenantId = tenantId,
                ProjectionName = IEventSessionCustomPropertyProjectionUpdater.ProjectionName
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.DrainedCount).IsEqualTo(2);
        await _sessionUpdater.Received(1).DrainDirtyScopesForTenantAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithUnknownProjectionName_ReturnsError()
    {
        var command = new DrainCustomPropertyProjectionDirtyScopesCommand
        {
            RequestDto = new DrainDirtyScopesRequestDto
            {
                TenantId = Guid.NewGuid(),
                ProjectionName = "unknown_projection"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message!).Contains("Unknown projection name");
    }

    [Test]
    public async Task Handle_WithEmptyTenantId_ReturnsValidationError()
    {
        var command = new DrainCustomPropertyProjectionDirtyScopesCommand
        {
            RequestDto = new DrainDirtyScopesRequestDto
            {
                TenantId = Guid.Empty,
                ProjectionName = IEventCustomPropertyProjectionUpdater.ProjectionName
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_WithEmptyProjectionName_ReturnsValidationError()
    {
        var command = new DrainCustomPropertyProjectionDirtyScopesCommand
        {
            RequestDto = new DrainDirtyScopesRequestDto
            {
                TenantId = Guid.NewGuid(),
                ProjectionName = ""
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_DrainReturnsZero_IsIdempotent()
    {
        var tenantId = Guid.NewGuid();
        _eventUpdater
            .DrainDirtyScopesForTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(0);

        var command = new DrainCustomPropertyProjectionDirtyScopesCommand
        {
            RequestDto = new DrainDirtyScopesRequestDto
            {
                TenantId = tenantId,
                ProjectionName = IEventCustomPropertyProjectionUpdater.ProjectionName
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id!.DrainedCount).IsEqualTo(0);
    }
}
