using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Features.LocationRooms.Handlers.Queries;
using Explore.Application.Features.LocationRooms.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.LocationRooms.Queries;

public class GetLocationRoomDetailRequestHandlerTests
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IMapper _mapper;
    private readonly GetLocationRoomDetailRequestHandler _handler;

    public GetLocationRoomDetailRequestHandlerTests()
    {
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetLocationRoomDetailRequestHandler(_locationRoomRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingRoom_ReturnsDto()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var request = new GetLocationRoomDetailRequest { Id = roomId };

        var room = DataBuilder.LocationRoom.Generate();
        room.Id = roomId;
        room.Name = "Main Hall";

        var expectedDto = new LocationRoomDto
        {
            Id = roomId,
            Name = "Main Hall"
        };

        _locationRoomRepository.GetById(roomId).Returns(room);
        _mapper.Map<LocationRoomDto>(room).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(roomId);
        await Assert.That(result.Name).IsEqualTo("Main Hall");
    }

    [Test]
    public async Task Handle_WithNonExistentRoom_ReturnsNull()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var request = new GetLocationRoomDetailRequest { Id = roomId };

        _locationRoomRepository.GetById(roomId).Returns((LocationRoom?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }
}
