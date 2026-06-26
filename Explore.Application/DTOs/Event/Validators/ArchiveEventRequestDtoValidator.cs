// ABOUTME: FluentValidation validator for ArchiveEventRequestDto, manually instantiated by handlers.
// ABOUTME: Enforces the optimistic-concurrency stamp required for safe archive transitions.

using Explore.Application.DTOs.Event;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public sealed class ArchiveEventRequestDtoValidator : AbstractValidator<ArchiveEventRequestDto>
{
    public ArchiveEventRequestDtoValidator()
    {
        RuleFor(x => x.ExpectedConcurrencyStamp)
            .NotEqual(Guid.Empty)
            .WithMessage("A non-empty concurrency stamp is required to archive an event.");
    }
}
