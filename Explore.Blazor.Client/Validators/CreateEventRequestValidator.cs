// ABOUTME: FluentValidation validator for CreateEventDraftRequestDto used in the Create Event form.
// ABOUTME: Validates required fields and selection constraints for event creation.

using Explore.Blazor.Client.Clients;
using FluentValidation;

namespace Explore.Blazor.Client.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventDraftRequestDto>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required.")
            .MaximumLength(200).WithMessage("Event title cannot exceed 200 characters.");

        RuleFor(x => x.Subtitle)
            .MaximumLength(200).WithMessage("Event subtitle cannot exceed 200 characters.");

        RuleFor(x => x.EventTypeId)
            .GreaterThan(0).WithMessage("Please select an event type.");

        RuleFor(x => x.AudienceGenderId)
            .GreaterThan(0).WithMessage("Please select target gender.");

        RuleFor(x => x.AudienceAgeId)
            .GreaterThan(0).WithMessage("Please select target age group.");

        RuleFor(x => x.VisibilityTypeId)
            .GreaterThan(0).WithMessage("Please select visibility type.");
    }
}
