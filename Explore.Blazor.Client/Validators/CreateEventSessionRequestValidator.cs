using Explore.Blazor.Client.Models.EventSessions;
using FluentValidation;

namespace Explore.Blazor.Client.Validators;

public class CreateEventSessionRequestValidator : AbstractValidator<CreateEventSessionRequest>
{
    public CreateEventSessionRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime);
        RuleFor(x => x.EventId).NotEmpty();
    }
}
