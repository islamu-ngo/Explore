using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.DTOs.Organization.Validators;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Organizations.Validators;

public class CreateOrganizationDtoValidatorTests
{
    private readonly IApprovalStatusRepository _approvalStatusRepository;
    private readonly CreateOrganizationDtoValidator _validator;

    public CreateOrganizationDtoValidatorTests()
    {
        _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
        _validator = new CreateOrganizationDtoValidator(_approvalStatusRepository);
    }

    [Test]
    public async Task Validate_WithValidDto_ReturnsValid()
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "https://example.com"
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
        var dto = new CreateOrganizationDto
        {
            FullName = "",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "FullName")).IsTrue();
    }

    [Test]
    public async Task Validate_WithFullNameExceedingMaxLength_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = new string('a', 101), // Exceeds 100 character limit
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "FullName")).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyEmail_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Email")).IsTrue();
    }

    [Test]
    [Arguments("invalid-email")]
    [Arguments("invalid@")]
    [Arguments("@invalid.com")]
    [Arguments("invalid")]
    public async Task Validate_WithInvalidEmail_ReturnsInvalid(string email)
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = email,
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Email")).IsTrue();
    }

    [Test]
    [Arguments("test@example.com")]
    [Arguments("user@domain.org")]
    [Arguments("name.surname@company.co.uk")]
    public async Task Validate_WithValidEmail_ReturnsValid(string email)
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = email,
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyCountry_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
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
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "City")).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyAddress_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "",
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Address")).IsTrue();
    }

    [Test]
    [Arguments("https://example.com")]
    [Arguments("http://example.org")]
    [Arguments("https://www.example.co.uk/page")]
    public async Task Validate_WithValidWebsiteUrl_ReturnsValid(string websiteUrl)
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = websiteUrl
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    [Arguments("not-a-url")]
    [Arguments("www.example.com")]
    public async Task Validate_WithInvalidWebsiteUrl_ReturnsInvalid(string websiteUrl)
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = websiteUrl
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "WebsiteUrl")).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyWebsiteUrl_ReturnsValid()
    {
        // Arrange - Website URL is optional
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = "123 Test Street",
            Postcode = 1000,
            WebsiteUrl = ""
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithAddressExceedingMaxLength_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateOrganizationDto
        {
            FullName = "Test Organization",
            Email = "test@example.com",
            Country = "Belgium",
            City = "Brussels",
            Address = new string('a', 201), // Exceeds 200 character limit
            Postcode = 1000,
            WebsiteUrl = "" // Always provide to avoid null reference in validator
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Address")).IsTrue();
    }
}
