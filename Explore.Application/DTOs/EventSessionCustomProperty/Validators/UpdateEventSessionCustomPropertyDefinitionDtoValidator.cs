// ABOUTME: Validates session runtime custom property definition update, extends create with Id check.
// ABOUTME: Manually instantiated in handlers (no DI), following project convention.

using FluentValidation;

namespace Explore.Application.DTOs.EventSessionCustomProperty.Validators;

public class UpdateEventSessionCustomPropertyDefinitionDtoValidator : AbstractValidator<UpdateEventSessionCustomPropertyDefinitionDto>
{
    public UpdateEventSessionCustomPropertyDefinitionDtoValidator()
    {
        Include(new CreateEventSessionCustomPropertyDefinitionDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
