// ABOUTME: FluentValidation validator for UpdateEventSeriesDto.
// ABOUTME: Manually instantiated in UpdateEventSeriesCommandHandler (not DI-injected).

using Explore.Application.DTOs.EventSeries;
using FluentValidation;

namespace Explore.Application.DTOs.EventSeries.Validators;

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
