// ABOUTME: Unit tests for draft event update validation around card summaries and long-form content.
// ABOUTME: Verifies the event description/content length split before update handlers mutate persisted events.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Validators;

public sealed class UpdateEventDraftRequestDtoValidatorTests
{
    private readonly IAudienceAgeRepository _audienceAgeRepository = Substitute.For<IAudienceAgeRepository>();
    private readonly IAudienceGenderRepository _audienceGenderRepository = Substitute.For<IAudienceGenderRepository>();
    private readonly IEventTypeRepository _eventTypeRepository = Substitute.For<IEventTypeRepository>();
    private readonly IVisibilityTypeRepository _visibilityTypeRepository = Substitute.For<IVisibilityTypeRepository>();
    private readonly IEventFormatRepository _eventFormatRepository = Substitute.For<IEventFormatRepository>();
    private readonly IStorageObjectRepository _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
    private readonly IEventSeriesRepository _eventSeriesRepository = Substitute.For<IEventSeriesRepository>();
    private readonly IEventRegistrationPolicyRepository _eventRegistrationPolicyRepository = Substitute.For<IEventRegistrationPolicyRepository>();
    private readonly UpdateEventDraftRequestDtoValidator _validator;

    public UpdateEventDraftRequestDtoValidatorTests()
    {
        _validator = new UpdateEventDraftRequestDtoValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _visibilityTypeRepository,
            _eventFormatRepository,
            _storageObjectRepository,
            _eventSeriesRepository,
            _eventRegistrationPolicyRepository);
    }

    [Test]
    public async Task Validate_WithDescriptionOver150Characters_ReturnsDescriptionError()
    {
        var request = CreateValidRequest();
        request.Description = new string('a', 151);

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpdateEventDraftRequestDto.Description))).IsTrue();
    }

    [Test]
    public async Task Validate_WithContentOver5000Characters_ReturnsContentError()
    {
        var request = CreateValidRequest();
        request.Content = new string('a', 5001);

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == nameof(UpdateEventDraftRequestDto.Content))).IsTrue();
    }

    [Test]
    public async Task Validate_WithDescriptionAndContentAtLimits_ReturnsTrue()
    {
        var request = CreateValidRequest();
        request.Description = new string('a', 150);
        request.Content = new string('b', 5000);

        var result = await _validator.ValidateAsync(request);

        await Assert.That(result.IsValid).IsTrue();
    }

    private UpdateEventDraftRequestDto CreateValidRequest()
    {
        var request = new UpdateEventDraftRequestDto
        {
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            ExpectedParticipationConfigurationConcurrencyStamp = Guid.NewGuid(),
            ParticipationConfiguration = new ConfigureEventParticipationDto
            {
                ParticipationHandlingModeId = 1,
                AdvanceRegistrationObligationId = 1
            },
            Title = "Draft event",
            VisibilityTypeId = 1,
            EventFormatId = 1
        };

        _visibilityTypeRepository.Exists(request.VisibilityTypeId).Returns(true);
        _eventFormatRepository.Exists(request.EventFormatId).Returns(true);

        return request;
    }
}
