using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventDays.Handlers.Commands;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventDays.Commands;

public class DeleteEventDayCommandHandlerTests
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly DeleteEventDayCommandHandler _handler;

    public DeleteEventDayCommandHandlerTests()
    {
        _eventDayRepository = Substitute.For<IEventDayRepository>();
        _handler = new DeleteEventDayCommandHandler(_eventDayRepository);
    }

    [Test]
    public async Task Handle_WithExistingEventDay_ReturnsSuccessResponse()
    {
        // Arrange
        var eventDayId = Guid.NewGuid();
        var command = new DeleteEventDayCommand { Id = eventDayId };

        var existingDay = new EventDay { Id = eventDayId, Event = null!, Tenant = null! };
        _eventDayRepository.GetById(eventDayId).Returns(existingDay);

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
}
