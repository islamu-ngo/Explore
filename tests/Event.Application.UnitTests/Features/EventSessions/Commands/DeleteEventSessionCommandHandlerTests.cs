// ABOUTME: Verifies event-session deletion preserves published ticket entitlement targets.
// ABOUTME: Covers successful deletion and rejection when a published catalog references the session.

using Event.Application.UnitTests.Features.EventTicketing;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessions.Handlers.Commands;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessions.Commands;

public sealed class DeleteEventSessionCommandHandlerTests
{
    private readonly IEventSessionRepository _sessions = Substitute.For<IEventSessionRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IEventLocationRepository _eventLocations = Substitute.For<IEventLocationRepository>();

    [Test]
    public async Task Handle_WhenSessionIsUnreferenced_DeletesLockedSession()
    {
        EventSession session = CreateSession();
        ConfigureSession(session);

        var result = await CreateHandler().Handle(
            new DeleteEventSessionCommand { Id = session.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(session.Id);
        await _sessions.Received(1).Delete(session);
    }

    [Test]
    public async Task Handle_WhenPublishedTicketReferencesSession_RejectsDeletion()
    {
        EventSession session = CreateSession();
        ConfigureSession(session);
        _catalogs.GetPublishedForUpdateAsync(
            session.EventId,
            session.TenantId,
            Arg.Any<CancellationToken>()).Returns(CreatePublishedCatalog(session));

        var result = await CreateHandler().Handle(
            new DeleteEventSessionCommand { Id = session.Id },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_session_ticket_entitlement_conflict");
        await _sessions.DidNotReceive().Delete(Arg.Any<EventSession>());
    }

    private void ConfigureSession(EventSession session)
    {
        _sessions.GetById(session.Id).Returns(session);
        _sessions.GetByIdForEventForUpdateAsync(
            session.Id,
            session.EventId,
            session.TenantId,
            Arg.Any<CancellationToken>()).Returns(session);
    }

    private DeleteEventSessionCommandHandler CreateHandler()
    {
        IUserContext user = Substitute.For<IUserContext>();
        ITenantContext tenant = Substitute.For<ITenantContext>();
        var locationService = new EventLocationAttachmentService(
            _eventLocations,
            user,
            tenant,
            TimeProvider.System);
        return new DeleteEventSessionCommandHandler(
            _sessions,
            _catalogs,
            new TicketingTestUnitOfWork(),
            locationService);
    }

    private static EventSession CreateSession() => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Event = null!,
        Tenant = null!
    };

    private static EventTicketCatalogVersion CreatePublishedCatalog(EventSession session)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(
            session.TenantId,
            session.EventId,
            "USD",
            1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(),
            session.TenantId,
            catalog.Id,
            "General",
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
        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEventSession(
            ticketType.Id,
            session,
            1,
            EntitlementSelectionRuleEnum.FixedSelection));
        catalog.Publish();
        return catalog;
    }
}
