// ABOUTME: Validates event template update payload, extends create validation with Id check.
// ABOUTME: Manually instantiated in handlers (no DI), following project convention.

using FluentValidation;

namespace Explore.Application.DTOs.EventTemplate.Validators;

public class UpdateEventTemplateDtoValidator : AbstractValidator<UpdateEventTemplateDto>
{
    public UpdateEventTemplateDtoValidator()
    {
        Include(new CreateEventTemplateDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
