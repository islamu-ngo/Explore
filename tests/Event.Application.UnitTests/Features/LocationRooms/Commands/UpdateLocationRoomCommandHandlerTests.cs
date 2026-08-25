// ABOUTME: Unit tests for grouped LocationRoom update command handling.
// ABOUTME: Covers validation, optimistic concurrency, tenant-safe location moves, and explicit field updates.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.LocationRoom;
using Explore.Application.Exceptions;
using Explore.Application.Features.LocationRooms.Handlers.Commands;
using Explore.Application.Features.LocationRooms.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.LocationRooms.Commands;

public class UpdateLocationRoomCommandHandlerTests
{
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly UpdateLocationRoomCommandHandler _handler;

    public UpdateLocationRoomCommandHandlerTests()
    {
        _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
        _locationRepository = Substitute.For<ILocationRepository>();

        _handler = new UpdateLocationRoomCommandHandler(
            _locationRoomRepository,
            _locationRepository
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
            LocationRoomId = roomId,
            UpdateLocationRoomDto = new UpdateLocationRoomDto
            {
                Location = new UpdateLocationRoomLocationDto { LocationId = locationId },
                Name = new UpdateLocationRoomNameDto { Value = "Updated Hall" },
                Capacity = new UpdateLocationRoomCapacityDto { Value = OptionalUpdate<int?>.Set(300) },
                SortOrder = new UpdateLocationRoomSortOrderDto { Value = 2 }
            }
        };

        var existingRoom = DataBuilder.LocationRoom.Generate();
        existingRoom.Id = roomId;
        existingRoom.TenantId = tenantId;
        existingRoom.ConcurrencyStamp = Guid.NewGuid();
        command = command with { ExpectedConcurrencyStamp = existingRoom.ConcurrencyStamp };
        _locationRoomRepository.GetById(roomId).Returns(existingRoom);

        var parentLocation = DataBuilder.Location.Generate();
        parentLocation.Id = locationId;
        parentLocation.TenantId = tenantId;
        _locationRepository.GetById(locationId).Returns(parentLocation);
        _locationRepository.Exists(locationId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(existingRoom.Name).IsEqualTo("Updated Hall");
        await Assert.That(existingRoom.Capacity).IsEqualTo(300);
        await Assert.That(existingRoom.SortOrder).IsEqualTo(2);
        await _locationRoomRepository.Received(1).Update(Arg.Any<LocationRoom>());
    }

    [Test]
    public async Task Handle_WhenWrapperHasNoGroups_ReturnsValidationFailureAndDoesNotSave()
    {
        var result = await _handler.Handle(new UpdateLocationRoomCommand
        {
            LocationRoomId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateLocationRoomDto = new UpdateLocationRoomDto()
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRoomRepository.DidNotReceive().Update(Arg.Any<LocationRoom>());
    }

    [Test]
    public async Task Handle_WithNonExistentRoom_ReturnsFailedResponse()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var command = new UpdateLocationRoomCommand
        {
            LocationRoomId = roomId,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateLocationRoomDto = new UpdateLocationRoomDto
            {
                Name = new UpdateLocationRoomNameDto { Value = "Ghost Room" }
            }
        };

        _locationRoomRepository.GetById(roomId).Returns((LocationRoom?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
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
            LocationRoomId = roomId,
            UpdateLocationRoomDto = new UpdateLocationRoomDto
            {
                Location = new UpdateLocationRoomLocationDto { LocationId = locationId },
                Name = new UpdateLocationRoomNameDto { Value = "Cross-tenant Room" }
            }
        };

        var existingRoom = DataBuilder.LocationRoom.Generate();
        existingRoom.Id = roomId;
        existingRoom.TenantId = Guid.NewGuid(); // Different tenant
        existingRoom.ConcurrencyStamp = Guid.NewGuid();
        command = command with { ExpectedConcurrencyStamp = existingRoom.ConcurrencyStamp };
        _locationRoomRepository.GetById(roomId).Returns(existingRoom);

        var parentLocation = DataBuilder.Location.Generate();
        parentLocation.Id = locationId;
        parentLocation.TenantId = Guid.NewGuid(); // Different tenant from room
        _locationRepository.GetById(locationId).Returns(parentLocation);
        _locationRepository.Exists(locationId).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRoomRepository.DidNotReceive().Update(Arg.Any<LocationRoom>());
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var room = DataBuilder.LocationRoom.Generate();
        room.Id = Guid.NewGuid();
        room.ConcurrencyStamp = Guid.NewGuid();
        _locationRoomRepository.GetById(room.Id).Returns(room);

        await Assert.That(async () => await _handler.Handle(new UpdateLocationRoomCommand
        {
            LocationRoomId = room.Id,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateLocationRoomDto = new UpdateLocationRoomDto
            {
                Name = new UpdateLocationRoomNameDto { Value = "Updated Hall" }
            }
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();

        await _locationRoomRepository.DidNotReceive().Update(Arg.Any<LocationRoom>());
    }
}
