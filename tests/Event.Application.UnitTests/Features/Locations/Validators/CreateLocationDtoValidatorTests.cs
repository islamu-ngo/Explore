using Explore.Application.DTOs.Location;
using Explore.Application.DTOs.Location.Validators;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Locations.Validators;

public class CreateLocationDtoValidatorTests
{
    private readonly CreateLocationDtoValidator _validator;

    public CreateLocationDtoValidatorTests()
    {
        _validator = new CreateLocationDtoValidator();
    }

    [Test]
    public async Task Validate_WithValidDto_ReturnsValid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels",
            Latitude = 50.8476,
            Longitude = 4.3572
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyFullName_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "FullName")).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyAddress_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Address")).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyPostcode_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "",
            Country = "Belgium",
            City = "Brussels"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Postcode")).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyCountry_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "",
            City = "Brussels"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Country")).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyCity_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = ""
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "City")).IsTrue();
    }

    [Test]
    [Arguments(-91)]
    [Arguments(91)]
    [Arguments(-100)]
    [Arguments(100)]
    public async Task Validate_WithInvalidLatitude_ReturnsInvalid(double latitude)
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels",
            Latitude = latitude,
            Longitude = 4.3572
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Latitude")).IsTrue();
    }

    [Test]
    [Arguments(-181)]
    [Arguments(181)]
    [Arguments(-200)]
    [Arguments(200)]
    public async Task Validate_WithInvalidLongitude_ReturnsInvalid(double longitude)
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels",
            Latitude = 50.8476,
            Longitude = longitude
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Longitude")).IsTrue();
    }

    [Test]
    [Arguments(-90)]
    [Arguments(90)]
    [Arguments(0)]
    [Arguments(45.5)]
    public async Task Validate_WithValidLatitude_ReturnsValid(double latitude)
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels",
            Latitude = latitude,
            Longitude = 4.3572
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments(-180)]
    [Arguments(180)]
    [Arguments(0)]
    [Arguments(100.5)]
    public async Task Validate_WithValidLongitude_ReturnsValid(double longitude)
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels",
            Latitude = 50.8476,
            Longitude = longitude
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithNullCoordinates_ReturnsValid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = "Test Location",
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels",
            Latitude = null,
            Longitude = null
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithFullNameExceedingMaxLength_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateLocationDto
        {
            FullName = new string('a', 501), // Exceeds 500 character limit
            Address = "123 Test Street",
            Postcode = "12345",
            Country = "Belgium",
            City = "Brussels"
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "FullName")).IsTrue();
    }
}
