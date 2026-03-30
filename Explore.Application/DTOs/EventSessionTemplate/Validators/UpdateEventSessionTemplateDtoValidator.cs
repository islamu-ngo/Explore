// ABOUTME: Validates event session template update payload, extends create validation with Id check.
// ABOUTME: Manually instantiated in handlers (no DI), following project convention.

using FluentValidation;

namespace Explore.Application.DTOs.EventSessionTemplate.Validators;

public class UpdateEventSessionTemplateDtoValidator : AbstractValidator<UpdateEventSessionTemplateDto>
{
    public UpdateEventSessionTemplateDtoValidator()
    {
        Include(new CreateEventSessionTemplateDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
