// ABOUTME: Validates event-local definition update payload, extends create validation with Id check.
// ABOUTME: Manually instantiated in handlers (no DI), following project convention.

using FluentValidation;

namespace Explore.Application.DTOs.EventCustomProperty.Validators;

public class UpdateEventCustomPropertyDefinitionDtoValidator : AbstractValidator<UpdateEventCustomPropertyDefinitionDto>
{
    public UpdateEventCustomPropertyDefinitionDtoValidator()
    {
        Include(new CreateEventCustomPropertyDefinitionDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.ExpectedConcurrencyStamp)
            .NotEmpty().WithMessage("ExpectedConcurrencyStamp is required.");
    }
}
