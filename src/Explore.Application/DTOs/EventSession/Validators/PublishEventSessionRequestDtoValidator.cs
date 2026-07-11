// ABOUTME: FluentValidation rules for explicit event-session publish transitions.
// ABOUTME: Enforces optimistic concurrency before lifecycle readiness mutates session status.

using Explore.Application.DTOs.EventSession;
using FluentValidation;

namespace Explore.Application.DTOs.EventSession.Validators;

public sealed class PublishEventSessionRequestDtoValidator : AbstractValidator<PublishEventSessionRequestDto>
{
    public PublishEventSessionRequestDtoValidator()
    {
        RuleFor(x => x.ExpectedConcurrencyStamp)
            .NotEqual(Guid.Empty)
            .WithMessage("A non-empty concurrency stamp is required to publish an event session.");
    }
}
