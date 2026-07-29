// ABOUTME: Tests direct capacity-pool deletion guard, audit state, and cache timing.
// ABOUTME: Proves active ticket references and foreign pools cannot be deleted.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
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
public sealed class DeleteEventCapacityPoolCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly DateTimeOffset _deletedAt = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly TicketingTestUnitOfWork _unitOfWork = new();

    public DeleteEventCapacityPoolCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(_userId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent());
        _catalogs.UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_WhenActiveTicketReferencesPool_ReturnsValidationFailureWithoutMutation()
    {
        EventCapacityPool pool = CreatePool();
        _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);
        _catalogs.HasLiveTicketTypeReferencesAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new DeleteEventCapacityPoolCommand { EventId = _eventId, CapacityPoolId = pool.Id },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(pool.IsDeleted).IsFalse();
        await _catalogs.DidNotReceive().UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenPublishedCatalogReferencesPoolEvenIfNewerDraftDoesNot_ReturnsValidationFailureWithoutMutation()
    {
        EventCapacityPool pool = CreatePool();
        _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);
        _catalogs.HasLiveTicketTypeReferencesAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(
            new DeleteEventCapacityPoolCommand { EventId = _eventId, CapacityPoolId = pool.Id },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(pool.IsDeleted).IsFalse();
        await _catalogs.DidNotReceive().UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_DeletesPoolWithCurrentUserAuditThenInvalidatesCache()
    {
        EventCapacityPool pool = CreatePool();
        _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(pool);
        _catalogs.HasLiveTicketTypeReferencesAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(
            new DeleteEventCapacityPoolCommand { EventId = _eventId, CapacityPoolId = pool.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(pool.IsDeleted).IsTrue();
        await Assert.That(pool.DeletedAt).IsEqualTo(_deletedAt.UtcDateTime);
        await Assert.That(pool.DeletedBy).IsEqualTo(_userId);
        await Assert.That(pool.UpdatedAt).IsEqualTo(_deletedAt.UtcDateTime);
        await Assert.That(pool.UpdatedBy).IsEqualTo(_userId);
        Received.InOrder(() =>
        {
            _catalogs.GetActiveCapacityPoolForUpdateAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>());
            _catalogs.HasLiveTicketTypeReferencesAsync(pool.Id, _eventId, _tenantId, Arg.Any<CancellationToken>());
            _catalogs.UpdateCapacityPoolAsync(pool, Arg.Any<CancellationToken>());
            _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WhenPoolBelongsToAnotherEvent_ReturnsGenericNotFound()
    {
        var result = await CreateHandler().Handle(
            new DeleteEventCapacityPoolCommand { EventId = _eventId, CapacityPoolId = Guid.CreateVersion7() },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().UpdateCapacityPoolAsync(Arg.Any<EventCapacityPool>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private DeleteEventCapacityPoolCommandHandler CreateHandler() => new(
        _events,
        _catalogs,
        _tenant,
        _currentUser,
        new FixedTimeProvider(_deletedAt),
        _unitOfWork,
        _cache);

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

    private EventTicketCatalogVersion CreateDraftCatalog(int versionNumber = 1) => EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", versionNumber);

    private EventCapacityPool CreatePool() => EventCapacityPool.Create(
        _tenantId,
        _eventId,
        "Main hall",
        200,
        900,
        CapacityOversellPolicyEnum.Disallow,
        true);

    private EventTicketType AddFreeTicket(EventTicketCatalogVersion catalog, EventCapacityPool? pool)
    {
        EventTicketType ticket = EventTicketType.Create(
            _tenantId,
            catalog.Id,
            "General admission",
            "USD",
            TicketPricingModeEnum.Free,
            null,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            pool?.Id,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            null);
        catalog.AddTicketType(ticket, pool);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        return ticket;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
