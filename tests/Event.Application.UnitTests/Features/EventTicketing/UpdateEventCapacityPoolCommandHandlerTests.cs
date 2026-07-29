// ABOUTME: Tests direct capacity-pool updates with event-scoped lookup and cache ordering.
// ABOUTME: Proves full-field domain updates, validation boundaries, and foreign-pool masking.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTicketing;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTicketing.Handlers.Commands;
using Explore.Application.Features.EventTicketing.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = global::Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class UpdateEventCapacityPoolCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly TicketingTestUnitOfWork _unitOfWork = new();

    public UpdateEventCapacityPoolCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent());
        _catalogs.UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_WithAllFields_UpdatesThenInvalidatesCache()
    {
        EventCapacityPool pool = CreatePool();
        _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);

        var result = await CreateHandler().Handle(
            new UpdateEventCapacityPoolCommand { EventId = _eventId, CapacityPoolId = pool.Id, CapacityPool = FullPoolDto() },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(pool.Name).IsEqualTo("Main hall revised");
        await Assert.That(pool.MaximumQuantity).IsEqualTo(300);
        await Assert.That(pool.HoldDurationSeconds).IsEqualTo(1_200);
        await Assert.That(pool.CapacityOversellPolicyId).IsEqualTo((int)CapacityOversellPolicyEnum.Allow);
        await Assert.That(pool.IsActive).IsFalse();
        Received.InOrder(() =>
        {
            _catalogs.UpdateCapacityPoolAsync(pool, Arg.Any<CancellationToken>());
            _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WhenRepositoryReportsConcurrencyConflict_ReturnsConflictWithoutCacheInvalidation()
    {
        EventCapacityPool pool = CreatePool();
        _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);
        _catalogs.UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The capacity pool was modified by another request."));

        var result = await CreateHandler().Handle(
            new UpdateEventCapacityPoolCommand { EventId = _eventId, CapacityPoolId = pool.Id, CapacityPool = FullPoolDto() },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_concurrency_conflict");
        await Assert.That(result.Errors).Contains("The capacity pool was modified by another request.");
        await _catalogs.Received(1).UpdateCapacityPoolAsync(pool, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPoolBelongsToAnotherEvent_ReturnsGenericNotFoundWithoutCacheInvalidation()
    {
        Guid foreignPoolId = Guid.CreateVersion7();

        var result = await CreateHandler().Handle(
            new UpdateEventCapacityPoolCommand { EventId = _eventId, CapacityPoolId = foreignPoolId, CapacityPool = FullPoolDto() },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCapacityPoolIsInvalid_ReturnsValidationFailureWithoutPersistence()
    {
        EventCapacityPool pool = CreatePool();
        _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);

        var result = await CreateHandler().Handle(
            new UpdateEventCapacityPoolCommand
            {
                EventId = _eventId,
                CapacityPoolId = pool.Id,
            CapacityPool = new ManageEventCapacityPoolDto { Name = string.Empty, HoldDurationSeconds = 0 }
            },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(pool.Name).IsEqualTo("Main hall");
        await _catalogs.DidNotReceive().UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private UpdateEventCapacityPoolCommandHandler CreateHandler() => new(_events, _catalogs, _tenant, _unitOfWork, _cache);

    private DomainEvent CreatePlatformEvent() => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        Title = "Ticketing event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            _eventId,
            _tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            guestRecoveryPolicy: null,
            DateTime.UtcNow)
    };

    private EventCapacityPool CreatePool() => EventCapacityPool.Create(
        _tenantId,
        _eventId,
        "Main hall",
        200,
        900,
        CapacityOversellPolicyEnum.Disallow,
        true);

    private static ManageEventCapacityPoolDto FullPoolDto() => new()
    {
        Name = "Main hall revised",
        MaximumQuantity = 300,
        HoldDurationSeconds = 1_200,
        CapacityOversellPolicyId = (int)CapacityOversellPolicyEnum.Allow,
        IsActive = false
    };
}
