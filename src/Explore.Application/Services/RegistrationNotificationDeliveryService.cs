// ABOUTME: Resolves registration confirmation email eligibility and in-app fallback.
// ABOUTME: Prevents unverified identity-provider email from becoming product email.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public sealed class RegistrationNotificationDeliveryService(
    INotificationRepository notificationRepository,
    INotificationPreferenceResolver notificationPreferenceResolver)
    : IRegistrationNotificationDeliveryService
{
    public RegistrationNotificationEmailResolution ResolveRegistrationConfirmationEmail(User user)
    {
        var email = user.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return new RegistrationNotificationEmailResolution(
                RegistrationNotificationEmailStatus.MissingEmail,
                null);
        }

        if (user.EmailVerified != true)
        {
            return new RegistrationNotificationEmailResolution(
                RegistrationNotificationEmailStatus.UnverifiedIdentityEmail,
                null);
        }

        return new RegistrationNotificationEmailResolution(
            RegistrationNotificationEmailStatus.VerifiedEmail,
            email);
    }

    public async Task CreateRegistrationConfirmationFallbackAsync(
        User user,
        Guid tenantId,
        Guid eventId,
        Guid registrationIntentId,
        string eventTitle,
        CancellationToken cancellationToken)
    {
        var preference = await notificationPreferenceResolver.ResolveAsync(
            new NotificationPreferenceResolveRequest(
                tenantId,
                user.Id,
                null,
                null,
                NotificationPreferenceCategoryCodes.RegistrationStatus,
                NotificationPreferenceChannelCodes.InApp),
            cancellationToken);
        if (!preference.IsEnabled)
        {
            return;
        }

        var deduplicationKey = $"event-registration-intent:{registrationIntentId:N}:registration-confirmation:fallback";
        if (await notificationRepository.ExistsByDeduplicationKeyAsync(
                tenantId,
                user.Id,
                deduplicationKey,
                cancellationToken))
        {
            return;
        }

        await notificationRepository.Create(new Notification
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            UserId = user.Id,
            User = null!,
            NotificationTypeId = (int)NotificationTypeEnum.RegistrationConfirmed,
            NotificationType = null!,
            Title = "Registration created",
            Body = $"Your registration for {eventTitle} was created. Add a verified notification email to receive email updates.",
            DeduplicationKey = deduplicationKey,
            NotificationEntityTypeId = (int)NotificationEntityTypeEnum.EventRegistration,
            EntityId = registrationIntentId.ToString(),
            NotificationScopeId = (int)ActorTypeEnum.User,
            NotificationScope = null!,
            NotificationReasonId = (int)NotificationReasonEnum.System,
            CreatedAt = DateTime.UtcNow
        });
    }
}
