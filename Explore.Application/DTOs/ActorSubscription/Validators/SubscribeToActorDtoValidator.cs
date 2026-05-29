// ABOUTME: Validates subscribe-to-actor payload shape before handler-level tenant checks.
// ABOUTME: Manually instantiated per project convention instead of dependency injection.

using FluentValidation;

namespace Explore.Application.DTOs.ActorSubscription.Validators;

public class SubscribeToActorDtoValidator : AbstractValidator<SubscribeToActorDto>
{
    public SubscribeToActorDtoValidator()
    {
        RuleFor(dto => dto.TargetActorId)
            .NotEmpty().WithMessage("Target actor ID is required.");
    }
}
