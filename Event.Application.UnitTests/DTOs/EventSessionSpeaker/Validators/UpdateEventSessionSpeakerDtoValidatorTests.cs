// ABOUTME: Tests grouped event-session speaker update DTO validation rules.
// ABOUTME: Guards empty wrapper rejection and required group fields.

using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.DTOs.EventSessionSpeaker.Validators;

namespace Event.Application.UnitTests.DTOs.EventSessionSpeaker.Validators;

public sealed class UpdateEventSessionSpeakerDtoValidatorTests
{
    private readonly UpdateEventSessionSpeakerDtoValidator _validator = new();

    [Test]
    public async Task Validate_WithEmptyWrapper_ReturnsGroupRequiredError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventSessionSpeakerDto());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("At least one event session speaker update group must be provided.");
    }

    [Test]
    public async Task Validate_WithActorGroup_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new UpdateEventSessionSpeakerDto
        {
            Actor = new UpdateEventSessionSpeakerActorDto { ActorId = Guid.NewGuid() }
        });

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptySessionId_ReturnsSessionError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventSessionSpeakerDto
        {
            Session = new UpdateEventSessionSpeakerSessionDto { EventSessionId = Guid.Empty }
        });

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("EventSessionId is required.");
    }
}
