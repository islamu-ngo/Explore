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

public class CreateLocationRoomCommandHandlerTests
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;
    private readonly CreateLocationRoomCommandHandler _handler;

    public CreateLocationRoomCommandHandlerTests()
    {
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _locationRepository = Substitute.For<ILocationRepository>();
        _mapper = Substitute.For<IMapper>();

        _locationRoomRepository.Create(Arg.Any<LocationRoom>())
            .Returns(callInfo => callInfo.Arg<LocationRoom>());

        _handler = new CreateLocationRoomCommandHandler(
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
        var command = new CreateLocationRoomCommand
        {
            LocationRoomDto = new CreateLocationRoomDto
            {
                LocationId = locationId,
                Name = "Main Hall",
                Slug = "main-hall",
                Description = "Large conference hall",
                Capacity = 200,
                SortOrder = 1
            }
        };

        var parentLocation = DataBuilder.Location.Generate();
        parentLocation.Id = locationId;
        parentLocation.TenantId = tenantId;
        _locationRepository.GetById(locationId).Returns(parentLocation);
        _locationRepository.Exists(locationId).Returns(true);

        var room = new LocationRoom { Id = roomId, Name = "Main Hall", Location = null!, Tenant = null! };
        _mapper.Map<LocationRoom>(command.LocationRoomDto).Returns(room);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(roomId);
        await _locationRoomRepository.Received(1).Create(Arg.Any<LocationRoom>());
    }

    [Test]
    public async Task Handle_WithValidRequest_SetsTenantIdFromParentLocation()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateLocationRoomCommand
        {
            LocationRoomDto = new CreateLocationRoomDto
            {
                LocationId = locationId,
                Name = "Room A",
                SortOrder = 1
            }
        };

        var parentLocation = DataBuilder.Location.Generate();
        parentLocation.Id = locationId;
        parentLocation.TenantId = tenantId;
        _locationRepository.GetById(locationId).Returns(parentLocation);
        _locationRepository.Exists(locationId).Returns(true);

        LocationRoom? capturedRoom = null;
        var room = new LocationRoom { Id = Guid.NewGuid(), Name = "Room A", Location = null!, Tenant = null! };
        _mapper.Map<LocationRoom>(command.LocationRoomDto).Returns(room);
        _locationRoomRepository.When(r => r.Create(Arg.Any<LocationRoom>()))
            .Do(callInfo => capturedRoom = callInfo.Arg<LocationRoom>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(capturedRoom).IsNotNull();
        await Assert.That(capturedRoom!.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task Handle_WithNonExistentLocation_ReturnsFailedResponse()
    {
        // Arrange
        var nonExistentLocationId = Guid.NewGuid();
        var command = new CreateLocationRoomCommand
        {
            LocationRoomDto = new CreateLocationRoomDto
            {
                LocationId = nonExistentLocationId,
                Name = "Orphan Room",
                SortOrder = 1
            }
        };

        _locationRepository.GetById(nonExistentLocationId).Returns((Location?)null);
        _locationRepository.Exists(nonExistentLocationId).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRoomRepository.DidNotReceive().Create(Arg.Any<LocationRoom>());
    }
}
