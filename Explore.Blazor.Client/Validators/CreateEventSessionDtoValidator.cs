// ABOUTME: Validates generated event-session create requests used by the Blazor composer.
// ABOUTME: Keeps client-side validation aligned with the generated API contract.

using Explore.Blazor.Client.Clients;
using FluentValidation;

namespace Explore.Blazor.Client.Validators;

public sealed class CreateEventSessionDtoValidator : AbstractValidator<CreateEventSessionDto>
{
    public CreateEventSessionDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime);
        RuleFor(x => x.EventId).NotEmpty();
    }
}
