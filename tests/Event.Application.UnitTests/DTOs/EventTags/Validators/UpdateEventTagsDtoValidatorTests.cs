// ABOUTME: Tests grouped event-tag update DTO validation rules.
// ABOUTME: Guards empty wrapper rejection and required group fields.

using Explore.Application.DTOs.EventTags;
using Explore.Application.DTOs.EventTags.Validators;

namespace Event.Application.UnitTests.DTOs.EventTags.Validators;

public sealed class UpdateEventTagsDtoValidatorTests
{
    private readonly UpdateEventTagsDtoValidator _validator = new();

    [Test]
    public async Task Validate_WithEmptyWrapper_ReturnsGroupRequiredError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventTagsDto());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("At least one event tag update group must be provided.");
    }

    [Test]
    public async Task Validate_WithTagGroup_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new UpdateEventTagsDto
        {
            Tag = new UpdateEventTagsTagDto { TagId = Guid.NewGuid() }
        });

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyTagId_ReturnsTagError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventTagsDto
        {
            Tag = new UpdateEventTagsTagDto { TagId = Guid.Empty }
        });

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("TagId is required.");
    }
}
