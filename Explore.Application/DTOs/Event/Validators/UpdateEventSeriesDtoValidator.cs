using Explore.Application.DTOs.Event;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public class UpdateEventSeriesDtoValidator : AbstractValidator<UpdateEventSeriesDto>
{
    public UpdateEventSeriesDtoValidator()
    {
        RuleFor(p => p.Id)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(p => p.Title)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");
    }
}
