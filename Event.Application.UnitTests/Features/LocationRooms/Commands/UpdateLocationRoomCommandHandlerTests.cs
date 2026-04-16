using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Features.LocationRooms.Handlers.Commands;
using Explore.Application.Features.LocationRooms.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.LocationRooms.Commands;

public class UpdateLocationRoomCommandHandlerTests
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;
    private readonly UpdateLocationRoomCommandHandler _handler;

    public UpdateLocationRoomCommandHandlerTests()
    {
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _locationRepository = Substitute.For<ILocationRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new UpdateLocationRoomCommandHandler(
            _locationRoomRepository,
            _locationRepository,
            _mapper
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new UpdateLocationRoomCommand
        {
            LocationRoomDto = new UpdateLocationRoomDto
            {
                Id = roomId,
                LocationId = locationId,
                Name = "Updated Hall",
                Capacity = 300,
                SortOrder = 2
            }
        };

        var existingRoom = DataBuilder.LocationRoom.Generate();
        existingRoom.Id = roomId;
        existingRoom.TenantId = tenantId;
        _locationRoomRepository.GetById(roomId).Returns(existingRoom);

        var parentLocation = DataBuilder.Location.Generate();
        parentLocation.Id = locationId;
        parentLocation.TenantId = tenantId;
        _locationRepository.GetById(locationId).Returns(parentLocation);
        _locationRepository.Exists(locationId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsTrue();
        await _locationRoomRepository.Received(1).Update(Arg.Any<LocationRoom>());
    }

    [Test]
    public async Task Handle_WithNonExistentRoom_ReturnsFailedResponse()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var command = new UpdateLocationRoomCommand
        {
            LocationRoomDto = new UpdateLocationRoomDto
            {
                Id = roomId,
                LocationId = Guid.NewGuid(),
                Name = "Ghost Room",
                SortOrder = 1
            }
        };

        _locationRoomRepository.GetById(roomId).Returns((LocationRoom?)null);
        _locationRepository.Exists(command.LocationRoomDto.LocationId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _locationRoomRepository.DidNotReceive().Update(Arg.Any<LocationRoom>());
    }

    [Test]
    public async Task Handle_WithCrossTenantLocation_ReturnsFailedResponse()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var command = new UpdateLocationRoomCommand
        {
            LocationRoomDto = new UpdateLocationRoomDto
            {
                Id = roomId,
                LocationId = locationId,
                Name = "Cross-tenant Room",
                SortOrder = 1
            }
        };

        var existingRoom = DataBuilder.LocationRoom.Generate();
        existingRoom.Id = roomId;
        existingRoom.TenantId = Guid.NewGuid(); // Different tenant
        _locationRoomRepository.GetById(roomId).Returns(existingRoom);

        var parentLocation = DataBuilder.Location.Generate();
        parentLocation.Id = locationId;
        parentLocation.TenantId = Guid.NewGuid(); // Different tenant from room
        _locationRepository.GetById(locationId).Returns(parentLocation);
        _locationRepository.Exists(locationId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.Success).IsFalse();
        await _locationRoomRepository.DidNotReceive().Update(Arg.Any<LocationRoom>());
    }
}
