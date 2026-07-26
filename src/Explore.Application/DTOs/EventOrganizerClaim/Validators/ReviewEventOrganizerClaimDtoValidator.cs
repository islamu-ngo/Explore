// ABOUTME: Validates curator claim-review transitions before domain state changes.
// ABOUTME: Requires a supported decision, reason code, and concurrency token.

using FluentValidation;

namespace Explore.Application.DTOs.EventOrganizerClaim.Validators;

public sealed class ReviewEventOrganizerClaimDtoValidator : AbstractValidator<ReviewEventOrganizerClaimDto>
{
    public ReviewEventOrganizerClaimDtoValidator()
    {
        RuleFor(dto => dto.Decision).IsInEnum();
        RuleFor(dto => dto.ReasonCode).NotEmpty().MaximumLength(80);
        RuleFor(dto => dto.ExpectedConcurrencyStamp).NotEmpty();
    }
}
