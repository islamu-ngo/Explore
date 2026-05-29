// ABOUTME: Validates notification-level update payload shape before ownership checks.
// ABOUTME: Keeps allowed lookup IDs in application code so invalid writes fail closed.

using Explore.Domain.Enums;
using FluentValidation;

namespace Explore.Application.DTOs.ActorSubscription.Validators;

public class UpdateActorSubscriptionNotificationLevelDtoValidator : AbstractValidator<UpdateActorSubscriptionNotificationLevelDto>
{
    private static readonly int[] AllowedNotificationLevelIds =
    [
        (int)ActorSubscriptionNotificationLevelEnum.None,
        (int)ActorSubscriptionNotificationLevelEnum.All,
        (int)ActorSubscriptionNotificationLevelEnum.Personalized
    ];

    public UpdateActorSubscriptionNotificationLevelDtoValidator()
    {
        RuleFor(dto => dto.TargetActorId)
            .NotEmpty().WithMessage("Target actor ID is required.");

        RuleFor(dto => dto.NotificationLevelId)
            .Must(id => AllowedNotificationLevelIds.Contains(id))
            .WithMessage("Notification level is not supported.");

        RuleFor(dto => dto.ExpectedConcurrencyStamp)
            .NotEmpty().WithMessage("Expected concurrency stamp is required.");
    }
}
