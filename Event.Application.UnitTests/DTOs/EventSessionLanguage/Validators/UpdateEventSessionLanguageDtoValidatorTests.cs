// ABOUTME: Tests grouped event-session language update DTO validation rules.
// ABOUTME: Guards empty wrapper rejection and required group fields.

using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.DTOs.EventSessionLanguage.Validators;

namespace Event.Application.UnitTests.DTOs.EventSessionLanguage.Validators;

public sealed class UpdateEventSessionLanguageDtoValidatorTests
{
    private readonly UpdateEventSessionLanguageDtoValidator _validator = new();

    [Test]
    public async Task Validate_WithEmptyWrapper_ReturnsGroupRequiredError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventSessionLanguageDto());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("At least one event session language update group must be provided.");
    }

    [Test]
    public async Task Validate_WithLanguageGroup_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new UpdateEventSessionLanguageDto
        {
            Language = new UpdateEventSessionLanguageLanguageDto { LanguageId = 2 }
        });

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptySessionId_ReturnsSessionError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventSessionLanguageDto
        {
            Session = new UpdateEventSessionLanguageSessionDto { EventSessionId = Guid.Empty }
        });

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("EventSessionId is required.");
    }
}
