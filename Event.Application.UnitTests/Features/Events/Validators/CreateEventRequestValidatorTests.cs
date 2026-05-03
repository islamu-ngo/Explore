// ABOUTME: Unit tests for the canonical single-submit CreateEventRequest validator.
// ABOUTME: Covers visible create-page field validation and graph collection requirements.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Validators;

public class CreateEventRequestValidatorTests
{
    private readonly IAudienceAgeRepository _audienceAgeRepository = Substitute.For<IAudienceAgeRepository>();
    private readonly IAudienceGenderRepository _audienceGenderRepository = Substitute.For<IAudienceGenderRepository>();
    private readonly IEventTypeRepository _eventTypeRepository = Substitute.For<IEventTypeRepository>();
    private readonly IOrganizationRepository _organizationRepository = Substitute.For<IOrganizationRepository>();
    private readonly IGroupRepository _groupRepository = Substitute.For<IGroupRepository>();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IEventTemplateRepository _eventTemplateRepository = Substitute.For<IEventTemplateRepository>();
    private readonly IEventSeriesRepository _eventSeriesRepository = Substitute.For<IEventSeriesRepository>();
    private readonly IEventRegistrationPolicyRepository _eventRegistrationPolicyRepository = Substitute.For<IEventRegistrationPolicyRepository>();
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly IRegistrationModeRepository _registrationModeRepository = Substitute.For<IRegistrationModeRepository>();
    private readonly ILanguageRepository _languageRepository = Substitute.For<ILanguageRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly IScheduleItemKindRepository _scheduleItemKindRepository = Substitute.For<IScheduleItemKindRepository>();
    private readonly ILocationRoomRepository _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository = Substitute.For<IEventSessionTemplateRepository>();
    private readonly CreateEventRequestValidator _validator;

    public CreateEventRequestValidatorTests()
    {
        _validator = new CreateEventRequestValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _organizationRepository,
            _groupRepository,
            _storageObjectRepository,
            _eventTemplateRepository,
            _eventSeriesRepository,
            _eventRegistrationPolicyRepository,
            _locationRepository,
            _registrationModeRepository,
            _languageRepository,
            _categoryRepository,
            _tagRepository,
            _scheduleItemKindRepository,
            _locationRoomRepository,
            _eventSessionTemplateRepository);
    }

    [Test]
    public async Task Validate_WithValidRequest_ReturnsTrue()
    {
        var request = CreateValidRequest();

        _eventTypeRepository.Exists(request.EventTypeId!.Value).Returns(true);
        _audienceGenderRepository.Exists(request.AudienceGenderId!.Value).Returns(true);
        _audienceAgeRepository.Exists(request.AudienceAgeId!.Value).Returns(true);

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithNullOptionalLookups_ReturnsTrue()
    {
        var request = CreateValidRequest();
        request.EventTypeId = null;
        request.AudienceGenderId = null;
        request.AudienceAgeId = null;

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithMinimalImportShapedRequest_ReturnsTrue()
    {
        var request = new CreateEventRequest
        {
            Title = "Imported program",
            Sessions =
            [
                new CreateEventSessionRequest
                {
                    StartTime = DateTimeOffset.UtcNow.AddDays(1),
                    EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
                }
            ]
        };

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsTrue();
        await _eventTypeRepository.DidNotReceive().Exists(Arg.Any<int>());
        await _audienceGenderRepository.DidNotReceive().Exists(Arg.Any<int>());
        await _audienceAgeRepository.DidNotReceive().Exists(Arg.Any<int>());
        await _organizationRepository.DidNotReceive().Exists(Arg.Any<Guid>());
        await _groupRepository.DidNotReceive().Exists(Arg.Any<Guid>());
    }

    [Test]
    public async Task Validate_WithNoSessions_ReturnsError()
    {
        var request = CreateValidRequest();
        request.Sessions = [];

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Sessions")).IsTrue();
    }

    [Test]
    public async Task Validate_WithInvalidTempReference_ReturnsError()
    {
        var request = CreateValidRequest();
        request.Sessions[0].RoomTempKey = "missing-room";

        _eventTypeRepository.Exists(request.EventTypeId!.Value).Returns(true);
        _audienceGenderRepository.Exists(request.AudienceGenderId!.Value).Returns(true);
        _audienceAgeRepository.Exists(request.AudienceAgeId!.Value).Returns(true);

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("temp-key references are invalid", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task Validate_WithOrganizationAndGroupSet_ReturnsError()
    {
        var organizationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var request = CreateValidRequest();
        request.OrganizationId = organizationId;
        request.GroupId = groupId;

        _eventTypeRepository.Exists(request.EventTypeId!.Value).Returns(true);
        _audienceGenderRepository.Exists(request.AudienceGenderId!.Value).Returns(true);
        _audienceAgeRepository.Exists(request.AudienceAgeId!.Value).Returns(true);
        _organizationRepository.Exists(organizationId).Returns(true);
        _groupRepository.Exists(groupId).Returns(true);

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("cannot both be provided"))).IsTrue();
    }

    private static CreateEventRequest CreateValidRequest() => new()
    {
        Title = "Valid Event",
        EventTypeId = 1,
        AudienceGenderId = 1,
        AudienceAgeId = 1,
        Sessions =
        [
            new CreateEventSessionRequest
            {
                Title = "Opening Session",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
            }
        ]
    };
}
