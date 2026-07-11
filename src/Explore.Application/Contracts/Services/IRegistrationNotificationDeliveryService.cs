// ABOUTME: Application boundary for event registration notification delivery eligibility.
// ABOUTME: Separates verified product email use from in-app fallback notifications.

using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IRegistrationNotificationDeliveryService
{
    RegistrationNotificationEmailResolution ResolveRegistrationConfirmationEmail(User user);

    Task CreateRegistrationConfirmationFallbackAsync(
        User user,
        Guid tenantId,
        Guid eventId,
        Guid registrationIntentId,
        string eventTitle,
        CancellationToken cancellationToken);
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
