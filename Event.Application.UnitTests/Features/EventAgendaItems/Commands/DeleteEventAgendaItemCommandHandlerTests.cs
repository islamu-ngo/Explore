using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventAgendaItems.Handlers.Commands;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventAgendaItems.Commands;

public class DeleteEventAgendaItemCommandHandlerTests
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly DeleteEventAgendaItemCommandHandler _handler;

    public DeleteEventAgendaItemCommandHandlerTests()
    {
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _handler = new DeleteEventAgendaItemCommandHandler(_eventAgendaItemRepository);
    }

    [Test]
    public async Task Handle_WithExistingAgendaItem_ReturnsSuccessResponse()
    {
        // Arrange
        var agendaItemId = Guid.NewGuid();
        var command = new DeleteEventAgendaItemCommand { Id = agendaItemId };

        var existingItem = new EventAgendaItem
        {
            Id = agendaItemId,
            Title = "To Delete",
            Event = null!,
            Tenant = null!
        };
        _eventAgendaItemRepository.GetById(agendaItemId).Returns(existingItem);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(agendaItemId);
        await _eventAgendaItemRepository.Received(1).Delete(Arg.Any<EventAgendaItem>());
    }

    [Test]
    public async Task Handle_WithNonExistentAgendaItem_ReturnsFailedResponse()
    {
        // Arrange
        var agendaItemId = Guid.NewGuid();
        var command = new DeleteEventAgendaItemCommand { Id = agendaItemId };

        _eventAgendaItemRepository.GetById(agendaItemId).Returns((EventAgendaItem?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _eventAgendaItemRepository.DidNotReceive().Delete(Arg.Any<EventAgendaItem>());
    }
}
