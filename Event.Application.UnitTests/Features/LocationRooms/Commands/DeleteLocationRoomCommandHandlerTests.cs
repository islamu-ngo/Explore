using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.LocationRooms.Handlers.Commands;
using Explore.Application.Features.LocationRooms.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.LocationRooms.Commands;

public class DeleteLocationRoomCommandHandlerTests
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly DeleteLocationRoomCommandHandler _handler;

    public DeleteLocationRoomCommandHandlerTests()
    {
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _handler = new DeleteLocationRoomCommandHandler(_locationRoomRepository);
    }

    [Test]
    public async Task Handle_WithExistingRoom_ReturnsSuccessResponse()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var command = new DeleteLocationRoomCommand { Id = roomId };

        var existingRoom = new LocationRoom { Id = roomId, Name = "To Delete", Location = null!, Tenant = null! };
        _locationRoomRepository.GetById(roomId).Returns(existingRoom);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(roomId);
        await _locationRoomRepository.Received(1).Delete(Arg.Any<LocationRoom>());
    }

    [Test]
    public async Task Handle_WithNonExistentRoom_ReturnsFailedResponse()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var command = new DeleteLocationRoomCommand { Id = roomId };

        _locationRoomRepository.GetById(roomId).Returns((LocationRoom?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _locationRoomRepository.DidNotReceive().Delete(Arg.Any<LocationRoom>());
    }
}
