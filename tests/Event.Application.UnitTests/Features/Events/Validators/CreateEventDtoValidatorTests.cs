// ABOUTME: Unit tests for the canonical draft-friendly CreateEventDto validator.
// ABOUTME: Covers visible create-page field validation and optional program graph validation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Validators;

public class CreateEventDtoValidatorTests
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
    private readonly IMadhabRepository _madhabRepository = Substitute.For<IMadhabRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly IScheduleItemKindRepository _scheduleItemKindRepository = Substitute.For<IScheduleItemKindRepository>();
    private readonly IEventSessionKindRepository _eventSessionKindRepository = Substitute.For<IEventSessionKindRepository>();
    private readonly ILocationRoomRepository _locationRoomRepository = Substitute.For<ILocationRoomRepository>();
    private readonly IEventSessionTemplateRepository _eventSessionTemplateRepository = Substitute.For<IEventSessionTemplateRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly CreateEventDtoValidator _validator;

    public CreateEventDtoValidatorTests()
    {
        _validator = new CreateEventDtoValidator(
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
            _madhabRepository,
            _categoryRepository,
            _tagRepository,
            _scheduleItemKindRepository,
            _eventSessionKindRepository,
            _locationRoomRepository,
            _eventSessionTemplateRepository,
            _actorRepository);
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
        request = request with { EventTypeId = null };
        request = request with { AudienceGenderId = null };
        request = request with { AudienceAgeId = null };

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithMinimalDraftRequest_ReturnsTrue()
    {
        var request = new CreateEventDto
        {
            Title = "Imported program",
            ParticipationConfiguration = CreateParticipationConfiguration()
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
    public async Task Validate_WithDescriptionOver150Characters_ReturnsDescriptionError()
    {
        var request = new CreateEventDto
        {
            Title = "Draft with long card summary",
            ParticipationConfiguration = CreateParticipationConfiguration(),
            Description = new string('a', 151)
        };

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateEventDto.Description))).IsTrue();
    }

    [Test]
    public async Task Validate_WithContentOver5000Characters_ReturnsContentError()
    {
        var request = new CreateEventDto
        {
            Title = "Draft with long content",
            ParticipationConfiguration = CreateParticipationConfiguration(),
            Content = new string('a', 5001)
        };

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == nameof(CreateEventDto.Content))).IsTrue();
    }

    [Test]
    public async Task Validate_WithDescriptionAndContentAtLimits_ReturnsTrue()
    {
        var request = new CreateEventDto
        {
            Title = "Draft at content limits",
            ParticipationConfiguration = CreateParticipationConfiguration(),
            Description = new string('a', 150),
            Content = new string('b', 5000)
        };

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithNoSessions_ReturnsTrue()
    {
        var request = new CreateEventDto
        {
            Title = "Draft without sessions",
            ParticipationConfiguration = CreateParticipationConfiguration(),
            Sessions = []
        };

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Sessions")).IsFalse();
    }

    [Test]
    public async Task Validate_WithInvalidTempReference_ReturnsError()
    {
        var request = CreateValidRequest();
        request.Sessions[0] = request.Sessions[0] with { RoomTempKey = "missing-room" };

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
        request = request with { OrganizationId = organizationId };
        request = request with { GroupId = groupId };

        _eventTypeRepository.Exists(request.EventTypeId!.Value).Returns(true);
        _audienceGenderRepository.Exists(request.AudienceGenderId!.Value).Returns(true);
        _audienceAgeRepository.Exists(request.AudienceAgeId!.Value).Returns(true);
        _organizationRepository.Exists(organizationId).Returns(true);
        _groupRepository.Exists(groupId).Returns(true);

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.ErrorMessage.Contains("cannot both be provided"))).IsTrue();
    }

    private static CreateEventDto CreateValidRequest() => new()
    {
        Title = "Valid Event",
        EventTypeId = 1,
        AudienceGenderId = 1,
        AudienceAgeId = 1,
        ParticipationConfiguration = new ConfigureEventParticipationDto
        {
            ParticipationHandlingModeId = 1,
            AdvanceRegistrationObligationId = 1
        },
        Sessions =
        [
            new CreateEventGraphSessionDto
            {
                Title = "Opening Session",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
            }
        ]
    };

    private static ConfigureEventParticipationDto CreateParticipationConfiguration() => new()
    {
        ParticipationHandlingModeId = 1,
        AdvanceRegistrationObligationId = 1
    };
}
