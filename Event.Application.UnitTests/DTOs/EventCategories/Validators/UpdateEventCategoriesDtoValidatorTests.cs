// ABOUTME: Tests grouped event-category update DTO validation rules.
// ABOUTME: Guards empty wrapper rejection and required group fields.

using Explore.Application.DTOs.EventCategories;
using Explore.Application.DTOs.EventCategories.Validators;

namespace Event.Application.UnitTests.DTOs.EventCategories.Validators;

public sealed class UpdateEventCategoriesDtoValidatorTests
{
    private readonly UpdateEventCategoriesDtoValidator _validator = new();

    [Test]
    public async Task Validate_WithEmptyWrapper_ReturnsGroupRequiredError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventCategoriesDto());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("At least one event category update group must be provided.");
    }

    [Test]
    public async Task Validate_WithCategoryGroup_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new UpdateEventCategoriesDto
        {
            Category = new UpdateEventCategoriesCategoryDto { CategoryId = Guid.NewGuid() }
        });

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithEmptyEventId_ReturnsEventError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventCategoriesDto
        {
            Event = new UpdateEventCategoriesEventDto { EventId = Guid.Empty }
        });

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("EventId is required.");
    }
}
