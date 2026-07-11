// ABOUTME: Unit tests for grouped Location update command handling.
// ABOUTME: Covers validation, optimistic concurrency, PII-backed fields, and explicit clear semantics.

using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Exceptions;
using Explore.Application.Features.Locations.Handlers.Commands;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Locations.Commands;

public class UpdateLocationCommandHandlerTests
{
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly UpdateLocationCommandHandler _handler;

    public UpdateLocationCommandHandlerTests()
    {
        _handler = new UpdateLocationCommandHandler(_locationRepository);
    }

    [Test]
    public async Task Handle_WhenWrapperHasNoGroups_ReturnsValidationFailureAndDoesNotSave()
    {
        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateLocationDto = new UpdateLocationDto()
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>());
    }

    [Test]
    public async Task Handle_WhenSingleFieldGroupIsPresent_UpdatesOnlyThatField()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id).Returns(location);

        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                FullName = new UpdateLocationFullNameDto { Value = "Updated Venue" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(location.FullName).IsEqualTo("Updated Venue");
        await Assert.That(location.Address).IsEqualTo("Existing address");
        await _locationRepository.Received(1).Update(location);
    }

    [Test]
    public async Task Handle_WhenLatitudeExplicitlyClears_SetsLatitudeToNull()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id).Returns(location);

        var result = await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = location.ConcurrencyStamp,
            UpdateLocationDto = new UpdateLocationDto
            {
                Latitude = new UpdateLocationLatitudeDto
                {
                    Value = OptionalUpdate<double?>.Set(null)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(location.Latitude).IsNull();
        await _locationRepository.Received(1).Update(location);
    }

    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConflictAndDoesNotSave()
    {
        var location = CreateLocation();
        _locationRepository.GetById(location.Id).Returns(location);

        await Assert.That(async () => await _handler.Handle(new UpdateLocationCommand
        {
            LocationId = location.Id,
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateLocationDto = new UpdateLocationDto
            {
                FullName = new UpdateLocationFullNameDto { Value = "Updated Venue" }
            }
        }, CancellationToken.None)).Throws<ConcurrencyConflictException>();

        await _locationRepository.DidNotReceive().Update(Arg.Any<Location>());
    }

    private static Location CreateLocation()
    {
        var location = DataBuilder.Location.Generate();
        location.Id = Guid.NewGuid();
        location.FullName = "Existing Venue";
        location.ConcurrencyStamp = Guid.NewGuid();
        location.Pii = new LocationPii
        {
            LocationId = location.Id,
            Address = "Existing address",
            Postcode = "1000",
            Latitude = 50.8503,
            Longitude = 4.3517
        };
        return location;
    }
}
