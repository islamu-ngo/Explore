// ABOUTME: Unit tests for transactional event agenda item deletion.
// ABOUTME: Verifies missing-item handling and EventLocation detachment coordination.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteEventAgendaItemCommandHandler _handler;

    public DeleteEventAgendaItemCommandHandlerTests()
    {
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.Arg<CancellationToken>()));
        var eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForCreateEvent(
            Guid.NewGuid(),
            Guid.NewGuid());
        _handler = new DeleteEventAgendaItemCommandHandler(
            _eventAgendaItemRepository,
            _unitOfWork,
            eventLocationAttachmentService);
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
