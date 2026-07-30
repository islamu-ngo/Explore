// ABOUTME: Tests direct ticket-type deletion handler audit and not-found behavior.
// ABOUTME: Proves aggregate deletion, current-user attribution, deterministic clock use, and cache timing.

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
public sealed class DeleteEventTicketTypeCommandHandlerTests
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

    public DeleteEventTicketTypeCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(_userId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent());
        _catalogs.UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Handle_DeletesTicketWithCurrentUserAuditThenInvalidatesCache()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventTicketType ticket = AddFreeTicket(catalog);
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateHandler().Handle(
            new DeleteEventTicketTypeCommand { EventId = _eventId, TicketTypeId = ticket.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(ticket.IsDeleted).IsTrue();
        await Assert.That(ticket.DeletedAt).IsEqualTo(_deletedAt.UtcDateTime);
        await Assert.That(ticket.DeletedBy).IsEqualTo(_userId);
        await Assert.That(ticket.UpdatedAt).IsEqualTo(_deletedAt.UtcDateTime);
        await Assert.That(ticket.UpdatedBy).IsEqualTo(_userId);
        Received.InOrder(() =>
        {
            _catalogs.UpdateAsync(catalog, Arg.Any<CancellationToken>());
            _cache.RemoveAsync($"event:detail:{_eventId}", Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_WhenTicketBelongsToAnotherEvent_ReturnsGenericNotFoundWithoutCacheInvalidation()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateHandler().Handle(
            new DeleteEventTicketTypeCommand { EventId = _eventId, TicketTypeId = Guid.CreateVersion7() },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_not_found");
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCurrentUserIsMissing_DoesNotDeleteOrInvalidateCache()
    {
        EventTicketCatalogVersion catalog = CreateDraftCatalog();
        EventTicketType ticket = AddFreeTicket(catalog);
        _catalogs.GetDraftCatalogForUpdateAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new DeleteEventTicketTypeCommand { EventId = _eventId, TicketTypeId = ticket.Id },
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("event_ticketing_validation_failed");
        await Assert.That(ticket.IsDeleted).IsFalse();
        await _catalogs.DidNotReceive().UpdateAsync(Arg.Any<EventTicketCatalogVersion>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private DeleteEventTicketTypeCommandHandler CreateHandler() => new(
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

    private EventTicketCatalogVersion CreateDraftCatalog() => EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);

    private EventTicketType AddFreeTicket(EventTicketCatalogVersion catalog)
    {
        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(),
            _tenantId,
            catalog.Id,
            "General admission",
            "USD",
            TicketPricingModeEnum.Free,
            null,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            null,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            null);
        catalog.AddTicketType(ticket, null);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        return ticket;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
