using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Validators;

public class CreateEventDtoValidatorTests
{
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly CreateEventDtoValidator _validator;

    public CreateEventDtoValidatorTests()
    {
        _audienceAgeRepository = Substitute.For<IAudienceAgeRepository>();
        _audienceGenderRepository = Substitute.For<IAudienceGenderRepository>();
        _eventTypeRepository = Substitute.For<IEventTypeRepository>();
        _organizationRepository = Substitute.For<IOrganizationRepository>();
        _groupRepository = Substitute.For<IGroupRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();

        _validator = new CreateEventDtoValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _organizationRepository,
            _groupRepository,
            _storageObjectRepository
        );
    }

    [Test]
    public async Task Validate_WithValidDto_ReturnsTrue()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Valid Event",
            EventTypeId = 1,
            AudienceGenderId = 1,
            AudienceAgeId = 1
        };

        _eventTypeRepository.Exists(dto.EventTypeId.Value).Returns(true);
        _audienceGenderRepository.Exists(dto.AudienceGenderId.Value).Returns(true);
        _audienceAgeRepository.Exists(dto.AudienceAgeId.Value).Returns(true);

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithNullOptionalFields_ReturnsTrue()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Valid Event",
            EventTypeId = null,
            AudienceGenderId = null,
            AudienceAgeId = null
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyTitle_ReturnsError()
    {
        // Arrange
        var dto = new CreateEventDto { Title = "" };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Title")).IsTrue();
    }

    [Test]
    public async Task Validate_WithLongSubtitle_ReturnsError()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Valid Title",
            Subtitle = new string('a', 201) // Exceeds 200
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Subtitle")).IsTrue();
    }

    [Test]
    public async Task Validate_WithNonExistentReferences_ReturnsError()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Valid Event",
            EventTypeId = 99,
            AudienceGenderId = 99,
            AudienceAgeId = 99
        };

        _eventTypeRepository.Exists(dto.EventTypeId.Value).Returns(false);
        _audienceGenderRepository.Exists(dto.AudienceGenderId.Value).Returns(false);
        _audienceAgeRepository.Exists(dto.AudienceAgeId.Value).Returns(false);

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "EventTypeId")).IsTrue();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "AudienceGenderId")).IsTrue();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "AudienceAgeId")).IsTrue();
    }

    [Test]
    public async Task Validate_WithOrganizationAndGroupSet_ReturnsError()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Title = "Valid Event",
            OrganizationId = Guid.NewGuid(),
            GroupId = Guid.NewGuid()
        };

        _organizationRepository.Exists(dto.OrganizationId.Value).Returns(true);
        _groupRepository.Exists(dto.GroupId.Value).Returns(true);

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("cannot both be provided"))).IsTrue();
    }
}
