// ABOUTME: Creates fixed Event lifecycle email automation outbox rows.
// ABOUTME: Centralizes source/kind/dedup fields and matching NotificationIntent audit ownership.

using System.Net;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Domain;

using ApplicationNotificationCategory = Explore.Application.Notifications.NotificationCategory;

namespace Explore.Application.Services;

public sealed class EventLifecycleEmailOutboxFactory(INotificationOrchestrator notificationOrchestrator)
    : IEventLifecycleEmailOutboxFactory
{
    public const string RegistrationIntentSourceType = "event_registration_intent";
    public const string EventSourceType = "event";

    public EmailDispatchOutbox CreateRegistrationConfirmation(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle)
    {
        var title = NormalizeTitle(eventTitle);
        return CreateRegistrationLifecycleEmail(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            recipientEmail,
            EmailDispatchKind.RegistrationConfirmation,
            $"Registration received for {title}",
            $"Your registration for {title} has been received. We will keep you updated if any registration status changes.",
            $"Your registration for <strong>{Html(title)}</strong> has been received. We will keep you updated if any registration status changes.");
    }

    public EmailDispatchOutbox CreateRegistrationApproved(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle)
    {
        var title = NormalizeTitle(eventTitle);
        return CreateRegistrationLifecycleEmail(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            recipientEmail,
            EmailDispatchKind.RegistrationApproved,
            $"Registration approved for {title}",
            $"Your registration for {title} has been approved.",
            $"Your registration for <strong>{Html(title)}</strong> has been approved.");
    }

    public EmailDispatchOutbox CreateRegistrationRejected(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle)
    {
        var title = NormalizeTitle(eventTitle);
        return CreateRegistrationLifecycleEmail(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            recipientEmail,
            EmailDispatchKind.RegistrationRejected,
            $"Registration update for {title}",
            $"Your registration for {title} was not approved.",
            $"Your registration for <strong>{Html(title)}</strong> was not approved.");
    }

    public EmailDispatchOutbox CreateWaitlistPromoted(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle)
    {
        var title = NormalizeTitle(eventTitle);
        return CreateRegistrationLifecycleEmail(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            recipientEmail,
            EmailDispatchKind.WaitlistPromoted,
            $"You are confirmed for {title}",
            $"A place opened for {title}; your waitlisted registration has been promoted.",
            $"A place opened for <strong>{Html(title)}</strong>; your waitlisted registration has been promoted.");
    }

    public EmailDispatchOutbox CreateEventReminder(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle,
        DateTimeOffset startsAt)
    {
        var title = NormalizeTitle(eventTitle);
        var startsAtText = startsAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'");
        return CreateRegistrationLifecycleEmail(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            recipientEmail,
            EmailDispatchKind.EventReminder,
            $"Reminder: {title}",
            $"This is a reminder that {title} starts at {startsAtText}.",
            $"This is a reminder that <strong>{Html(title)}</strong> starts at {Html(startsAtText)}.");
    }

    public EmailDispatchOutbox CreateEventCancelled(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle)
    {
        var title = NormalizeTitle(eventTitle);
        return CreateRegistrationLifecycleEmail(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            recipientEmail,
            EmailDispatchKind.EventCancelled,
            $"Event cancelled: {title}",
            $"{title} has been cancelled. Please check the event page for any organizer updates.",
            $"<strong>{Html(title)}</strong> has been cancelled. Please check the event page for any organizer updates.");
    }

    public EmailDispatchOutbox CreateOrganizerNotification(
        Guid tenantId,
        Guid eventId,
        Guid organizerUserId,
        string recipientEmail,
        string eventTitle,
        string notificationSubject,
        string notificationBody)
    {
        var title = NormalizeTitle(eventTitle);
        var subject = string.IsNullOrWhiteSpace(notificationSubject)
            ? $"Organizer update for {title}"
            : notificationSubject.Trim();
        var body = string.IsNullOrWhiteSpace(notificationBody)
            ? $"There is an organizer update for {title}."
            : notificationBody.Trim();

        return new EmailDispatchOutbox
        {
            TenantId = tenantId,
            Kind = EmailDispatchKind.OrganizerNotification,
            SourceType = EventSourceType,
            SourceId = eventId,
            EventId = eventId,
            UserId = organizerUserId,
            RecipientEmail = NormalizeEmail(recipientEmail),
            Subject = subject,
            PlainTextBody = ComposePlainText(body),
            HtmlBody = ComposeHtmlFromText(body),
            CorrelationId = $"{eventId}:organizer:{organizerUserId}"
        };
    }

    public async Task EnqueueNotificationIntentAsync(
        EmailDispatchOutbox outbox,
        CancellationToken cancellationToken)
    {
        await notificationOrchestrator.EnqueueAsync(CreateNotificationIntentDraft(outbox), cancellationToken);
    }

    private static NotificationIntentDraft CreateNotificationIntentDraft(EmailDispatchOutbox outbox)
    {
        var templateKey = GetTemplateKey(outbox.Kind);
        var sourceReference = GetSourceReference(outbox);

        return new NotificationIntentDraft(
            GetCategory(outbox),
            TenantId: outbox.TenantId,
            RecipientKind: GetRecipientKind(outbox),
            TemplateKey: templateKey,
            SafePayloadReference: sourceReference,
            IsUserFacing: true,
            IsIslamuInitiated: true,
            DeduplicationKey: $"{sourceReference}:{templateKey.Replace('.', '-')}",
            CorrelationId: string.IsNullOrWhiteSpace(outbox.CorrelationId)
                ? outbox.PublishEventId.ToString()
                : outbox.CorrelationId,
            UserId: outbox.UserId,
            EventId: outbox.EventId);
    }

    private static ApplicationNotificationCategory GetCategory(EmailDispatchOutbox outbox)
    {
        return outbox.Kind == EmailDispatchKind.OrganizerNotification
            ? ApplicationNotificationCategory.EventLifecycle
            : ApplicationNotificationCategory.RegistrationLifecycle;
    }

    private static string GetRecipientKind(EmailDispatchOutbox outbox)
    {
        return outbox.Kind == EmailDispatchKind.OrganizerNotification ? "Organizer" : "User";
    }

    private static string GetSourceReference(EmailDispatchOutbox outbox)
    {
        var sourceType = outbox.SourceType switch
        {
            RegistrationIntentSourceType => "event-registration-intent",
            EventSourceType => "event",
            _ => outbox.SourceType
        };

        return $"{sourceType}:{outbox.SourceId}";
    }

    private static string GetTemplateKey(EmailDispatchKind kind)
    {
        return kind switch
        {
            EmailDispatchKind.RegistrationConfirmation => "registration.confirmation",
            EmailDispatchKind.RegistrationApproved => "registration.approved",
            EmailDispatchKind.RegistrationRejected => "registration.rejected",
            EmailDispatchKind.WaitlistPromoted => "registration.waitlist.promoted",
            EmailDispatchKind.EventReminder => "event.reminder",
            EmailDispatchKind.EventCancelled => "event.cancelled",
            EmailDispatchKind.OrganizerNotification => "event.organizer.notification",
            _ => throw new InvalidOperationException($"Unsupported event lifecycle email kind '{kind}'.")
        };
    }

    private static EmailDispatchOutbox CreateRegistrationLifecycleEmail(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        EmailDispatchKind kind,
        string subject,
        string plainTextBody,
        string htmlBody)
    {
        return new EmailDispatchOutbox
        {
            TenantId = tenantId,
            Kind = kind,
            SourceType = RegistrationIntentSourceType,
            SourceId = registrationIntentId,
            EventId = eventId,
            RegistrationIntentId = registrationIntentId,
            UserId = userId,
            RecipientEmail = NormalizeEmail(recipientEmail),
            Subject = subject,
            PlainTextBody = ComposePlainText(plainTextBody),
            HtmlBody = ComposeTrustedHtml(htmlBody),
            CorrelationId = registrationIntentId.ToString()
        };
    }

    private static string NormalizeTitle(string eventTitle)
    {
        return string.IsNullOrWhiteSpace(eventTitle) ? "the event" : eventTitle.Trim();
    }

    private static string NormalizeEmail(string recipientEmail)
    {
        return recipientEmail.Trim();
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string ComposePlainText(string body)
    {
        return $"Assalamu alaykum,\n\n{body}\n\nEvent Platform";
    }

    private static string ComposeTrustedHtml(string body)
    {
        return $"<p>Assalamu alaykum,</p><p>{body}</p><p>Event Platform</p>";
    }

    private static string ComposeHtmlFromText(string body)
    {
        var encodedBody = Html(body)
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal)
            .Replace("\r", "<br />", StringComparison.Ordinal);

        return ComposeTrustedHtml(encodedBody);
    }
}
