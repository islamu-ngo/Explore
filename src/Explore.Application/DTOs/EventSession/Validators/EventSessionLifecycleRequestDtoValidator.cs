// ABOUTME: FluentValidation rules for explicit event-session lifecycle transitions.
// ABOUTME: Enforces optimistic concurrency before archive, cancel, or complete mutates session status.

using Explore.Application.DTOs.EventSession;
using FluentValidation;

namespace Explore.Application.DTOs.EventSession.Validators;

public sealed class EventSessionLifecycleRequestDtoValidator : AbstractValidator<EventSessionLifecycleRequestDto>
{
    public EventSessionLifecycleRequestDtoValidator()
    {
        RuleFor(x => x.ExpectedConcurrencyStamp)
            .NotEqual(Guid.Empty)
            .WithMessage("A non-empty concurrency stamp is required to transition an event session.");
    }
}
