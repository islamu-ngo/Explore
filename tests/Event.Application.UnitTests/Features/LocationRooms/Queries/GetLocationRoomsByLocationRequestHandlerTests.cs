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

public class GetLocationRoomsByLocationRequestHandlerTests
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IMapper _mapper;
    private readonly GetLocationRoomsByLocationRequestHandler _handler;

    public GetLocationRoomsByLocationRequestHandlerTests()
    {
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetLocationRoomsByLocationRequestHandler(_locationRoomRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingRooms_ReturnsMappedList()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var request = new GetLocationRoomsByLocationRequest { LocationId = locationId };

        var rooms = new List<LocationRoom>
        {
            DataBuilder.LocationRoom.Generate(),
            DataBuilder.LocationRoom.Generate()
        };

        var expectedDtos = new List<LocationRoomListDto>
        {
            new() { Id = rooms[0].Id, Name = "Room A" },
            new() { Id = rooms[1].Id, Name = "Room B" }
        };

        _locationRoomRepository.GetByLocationAsync(locationId, Arg.Any<CancellationToken>()).Returns(rooms);
        _mapper.Map<List<LocationRoomListDto>>(rooms).Returns(expectedDtos);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Handle_WithNoRooms_ReturnsEmptyList()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var request = new GetLocationRoomsByLocationRequest { LocationId = locationId };

        _locationRoomRepository.GetByLocationAsync(locationId, Arg.Any<CancellationToken>())
            .Returns(new List<LocationRoom>());
        _mapper.Map<List<LocationRoomListDto>>(Arg.Any<List<LocationRoom>>())
            .Returns(new List<LocationRoomListDto>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }
}
