// ABOUTME: Validates template definition update payload, extends create validation with Id check.
// ABOUTME: Manually instantiated in handlers (no DI), following project convention.

using FluentValidation;

namespace Explore.Application.DTOs.EventTemplate.Validators;

public class UpdateEventTemplateDefinitionDtoValidator : AbstractValidator<UpdateEventTemplateDefinitionDto>
{
    public UpdateEventTemplateDefinitionDtoValidator()
    {
        Include(new CreateEventTemplateDefinitionDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
