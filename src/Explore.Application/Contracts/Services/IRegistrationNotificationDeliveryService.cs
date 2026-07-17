// ABOUTME: Application boundary for event registration notification delivery eligibility.
// ABOUTME: Separates verified product email use from in-app fallback notifications.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IRegistrationNotificationDeliveryService
{
    RegistrationNotificationEmailResolution ResolveRegistrationConfirmationEmail(User user);

    RecipientNotificationMaterialization? CreateLifecycleMaterialization(
        EventRegistrationIntent registrationIntent,
        string eventTitle,
        User user,
        EventRegistrationTransitionResult transition,
        Guid notificationIntentId,
        Guid emailDispatchOutboxId);
}

public sealed record RegistrationNotificationEmailResolution(
    RegistrationNotificationEmailStatus Status,
    string? Email)
{
    public bool HasVerifiedEmail => Status == RegistrationNotificationEmailStatus.VerifiedEmail;
}

public enum RegistrationNotificationEmailStatus
{
    VerifiedEmail = 1,
    MissingEmail = 2,
    UnverifiedIdentityEmail = 3
}
