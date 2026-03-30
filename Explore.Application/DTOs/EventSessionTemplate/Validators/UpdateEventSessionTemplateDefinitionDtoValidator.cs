// ABOUTME: Validates session template definition update payload, extends create with Id check.
// ABOUTME: Manually instantiated in handlers (no DI), following project convention.

using FluentValidation;

namespace Explore.Application.DTOs.EventSessionTemplate.Validators;

public class UpdateEventSessionTemplateDefinitionDtoValidator : AbstractValidator<UpdateEventSessionTemplateDefinitionDto>
{
    public UpdateEventSessionTemplateDefinitionDtoValidator()
    {
        Include(new CreateEventSessionTemplateDefinitionDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
