// ABOUTME: Application contract for fixed Event lifecycle email automation intents.
// ABOUTME: Produces EmailDispatchOutbox snapshots for atomic recipient-notification materialization.

using Explore.Domain;

namespace Explore.Application.Contracts.Services;

public interface IEventLifecycleEmailOutboxFactory
{
    EmailDispatchOutbox CreateRegistrationConfirmation(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle);

    EmailDispatchOutbox CreateRegistrationApproved(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle);

    EmailDispatchOutbox CreateRegistrationRejected(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle);

    EmailDispatchOutbox CreateWaitlistPromoted(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle);

    EmailDispatchOutbox CreateRegistrationCancelled(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle);

    EmailDispatchOutbox CreateRegistrationRevoked(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle);

    EmailDispatchOutbox CreateEventReminder(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle,
        DateTimeOffset startsAt);

    EmailDispatchOutbox CreateEventCancelled(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle);

    EmailDispatchOutbox CreateOrganizerNotification(
        Guid tenantId,
        Guid eventId,
        Guid organizerUserId,
        string recipientEmail,
        string eventTitle,
        string notificationSubject,
        string notificationBody);

}
