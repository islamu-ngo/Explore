using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventDays.Handlers.Commands;
using Explore.Application.Features.EventDays.Requests.Commands;
using Event.Application.UnitTests.Features.EventTicketing;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventDays.Commands;

public class DeleteEventDayCommandHandlerTests
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventTicketCatalogRepository _catalogs;
    private readonly DeleteEventDayCommandHandler _handler;

    public DeleteEventDayCommandHandlerTests()
    {
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _catalogs = Substitute.For<IEventTicketCatalogRepository>();
        _handler = new DeleteEventDayCommandHandler(
            _eventDayRepository,
            _catalogs,
            new TicketingTestUnitOfWork());
    }

    [Test]
    public async Task Handle_WithExistingEventDay_ReturnsSuccessResponse()
    {
        // Arrange
        var eventDayId = Guid.NewGuid();
        var command = new DeleteEventDayCommand { Id = eventDayId };

        var existingDay = CreateEventDay(eventDayId);
        _eventDayRepository.GetById(eventDayId).Returns(existingDay);
        _eventDayRepository.GetByIdForEventForUpdateAsync(
            eventDayId,
            existingDay.EventId,
            existingDay.TenantId,
            Arg.Any<CancellationToken>()).Returns(existingDay);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventDayId);
        await _eventDayRepository.Received(1).Delete(Arg.Any<EventDay>());
    }

    [Test]
    public async Task Handle_WithNonExistentEventDay_ReturnsFailedResponse()
    {
        // Arrange
        var eventDayId = Guid.NewGuid();
        var command = new DeleteEventDayCommand { Id = eventDayId };

        _eventDayRepository.GetById(eventDayId).Returns((EventDay?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventDayRepository.DidNotReceive().Delete(Arg.Any<EventDay>());
    }

    [Test]
    public async Task Handle_WhenPublishedTicketReferencesDay_RejectsDeletion()
    {
        Guid eventDayId = Guid.CreateVersion7();
        EventDay existingDay = CreateEventDay(eventDayId);
        EventTicketCatalogVersion published = CreatePublishedCatalog(existingDay);
        _eventDayRepository.GetById(eventDayId).Returns(existingDay);
        _eventDayRepository.GetByIdForEventForUpdateAsync(
            eventDayId,
            existingDay.EventId,
            existingDay.TenantId,
            Arg.Any<CancellationToken>()).Returns(existingDay);
        _catalogs.GetPublishedForUpdateAsync(
            existingDay.EventId,
            existingDay.TenantId,
            Arg.Any<CancellationToken>()).Returns(published);

        var result = await _handler.Handle(new DeleteEventDayCommand { Id = eventDayId }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_day_ticket_entitlement_conflict");
        await Assert.That(result.Message).IsEqualTo("Event day is referenced by a published ticket catalog.");
        await _eventDayRepository.DidNotReceive().Delete(Arg.Any<EventDay>());
    }

    private static EventDay CreateEventDay(Guid id) => new()
    {
        Id = id,
        EventId = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Event = null!,
        Tenant = null!
    };

    private static EventTicketCatalogVersion CreatePublishedCatalog(EventDay eventDay)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(
            eventDay.TenantId,
            eventDay.EventId,
            "USD",
            1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(),
            eventDay.TenantId,
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
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEventDay(
            ticketType.Id,
            eventDay,
            1,
            EntitlementSelectionRuleEnum.FixedSelection));
        catalog.Publish();
        return catalog;
    }
}
