// ABOUTME: Tests grouped event registration update DTO validation rules.
// ABOUTME: Guards empty wrapper rejection and explicit field-operation semantics.

using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Application.Models.Common;

namespace Event.Application.UnitTests.DTOs.EventRegistration.Validators;

public sealed class UpdateEventRegistrationDtoValidatorTests
{
    private readonly UpdateEventRegistrationDtoValidator _validator = new();

    [Test]
    public async Task Validate_WithEmptyWrapper_ReturnsGroupRequiredError()
    {
        var result = await _validator.ValidateAsync(new UpdateEventRegistrationDto());

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("At least one event registration update group must be provided.");
    }

    [Test]
    public async Task Validate_WithApprovalStatusSet_ReturnsValid()
    {
        var dto = new UpdateEventRegistrationDto
        {
            ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto
            {
                ApprovalStatusId = OptionalUpdate<int?>.Set(1)
            }
        };

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_WithApprovalStatusGroupButNoOperation_ReturnsError()
    {
        var dto = new UpdateEventRegistrationDto
        {
            ApprovalStatus = new UpdateEventRegistrationApprovalStatusDto()
        };

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("ApprovalStatusId must specify an explicit field operation.");
    }

    [Test]
    public async Task Validate_WithEmptySessionId_ReturnsSessionError()
    {
        var dto = new UpdateEventRegistrationDto
        {
            Session = new UpdateEventRegistrationSessionDto { EventSessionId = Guid.Empty }
        };

        var result = await _validator.ValidateAsync(dto);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(error => error.ErrorMessage))
            .Contains("EventSessionId is required.");
    }
}
