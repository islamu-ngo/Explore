using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.Locations.Handlers.Commands;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Locations.Commands;

public class CreateLocationCommandHandlerTests
{
    private readonly ILocationRepository _locationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly CreateLocationCommandHandler _handler;

    public CreateLocationCommandHandlerTests()
    {
        _locationRepository = Substitute.For<ILocationRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IMapper>();

        _handler = new CreateLocationCommandHandler(
            _locationRepository,
            _tenantContext,
            _mapper
        );
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var command = new CreateLocationCommand
        {
            LocationDto = new CreateLocationDto
            {
                FullName = "Test Location",
                Address = "123 Test Street",
                Postcode = "12345",
                Country = "Belgium",
                City = "Brussels",
                Latitude = 50.8476,
                Longitude = 4.3572
            }
        };

        _tenantContext.TenantId.Returns(tenantId);

        var location = new Location { Id = locationId, FullName = "Test", Country = "BE", City = "Brussels", Pii = new LocationPii { Address = "Test", Postcode = "00000" }, Tenant = null! };
        _mapper.Map<Location>(command.LocationDto).Returns(location);
        _locationRepository.Create(Arg.Any<Location>()).Returns(location);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(locationId);
        await Assert.That(result.Message).Contains("successfully");
        await _locationRepository.Received(1).Create(Arg.Any<Location>());
    }

    [Test]
    public async Task Handle_WithMissingFullName_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateLocationCommand
        {
            LocationDto = new CreateLocationDto
            {
                FullName = "", // Missing required field
                Address = "123 Test Street",
                Postcode = "12345",
                Country = "Belgium",
                City = "Brussels"
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRepository.DidNotReceive().Create(Arg.Any<Location>());
    }

    [Test]
    public async Task Handle_WithMissingAddress_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateLocationCommand
        {
            LocationDto = new CreateLocationDto
            {
                FullName = "Test Location",
                Address = "", // Missing required field
                Postcode = "12345",
                Country = "Belgium",
                City = "Brussels"
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRepository.DidNotReceive().Create(Arg.Any<Location>());
    }

    [Test]
    public async Task Handle_WithInvalidCoordinates_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateLocationCommand
        {
            LocationDto = new CreateLocationDto
            {
                FullName = "Test Location",
                Address = "123 Test Street",
                Postcode = "12345",
                Country = "Belgium",
                City = "Brussels",
                Latitude = 200, // Invalid latitude (must be -90 to 90)
                Longitude = 4.3572
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await _locationRepository.DidNotReceive().Create(Arg.Any<Location>());
    }

    [Test]
    public async Task Handle_SetsTenantIdFromContext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var command = new CreateLocationCommand
        {
            LocationDto = new CreateLocationDto
            {
                FullName = "Test Location",
                Address = "123 Test Street",
                Postcode = "12345",
                Country = "Belgium",
                City = "Brussels"
            }
        };

        _tenantContext.TenantId.Returns(tenantId);

        var location = new Location { Id = locationId, FullName = "Test", Country = "BE", City = "Brussels", Pii = new LocationPii { Address = "Test", Postcode = "00000" }, Tenant = null! };
        _mapper.Map<Location>(command.LocationDto).Returns(location);
        _locationRepository.Create(Arg.Any<Location>()).Returns(location);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await _locationRepository.Received(1).Create(Arg.Is<Location>(l => l.TenantId == tenantId));
    }
}
