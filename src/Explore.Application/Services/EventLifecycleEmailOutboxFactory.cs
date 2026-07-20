// ABOUTME: Creates fixed Event lifecycle email automation outbox rows.
// ABOUTME: Centralizes source, kind, recipient-authority, and transport snapshot fields.

using System.Net;
using Explore.Application.Contracts.Services;
using Explore.Domain;

namespace Explore.Application.Services;

public sealed class EventLifecycleEmailOutboxFactory
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

    public EmailDispatchOutbox CreateRegistrationCancelled(
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
            EmailDispatchKind.RegistrationCancelled,
            $"Registration cancelled for {title}",
            $"Your registration for {title} has been cancelled as requested.",
            $"Your registration for <strong>{Html(title)}</strong> has been cancelled as requested.");
    }

    public EmailDispatchOutbox CreateRegistrationRevoked(
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
            EmailDispatchKind.RegistrationRevoked,
            $"Registration update for {title}",
            $"Your registration for {title} is no longer active. Contact the event organizer if you need more information.",
            $"Your registration for <strong>{Html(title)}</strong> is no longer active. Contact the event organizer if you need more information.");
    }

    public EmailDispatchOutbox CreateEventReminder(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        Guid registrationIntentId,
        string recipientEmail,
        string eventTitle,
        DateTimeOffset startsAtUtc,
        string timeZoneId = "UTC")
    {
        var title = NormalizeTitle(eventTitle);
        string startsAtText = EventReminderAuthorityReference.FormatDisplay(startsAtUtc, timeZoneId);
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
            RecipientUserId = organizerUserId,
            RecipientEmail = NormalizeEmail(recipientEmail),
            Subject = subject,
            PlainTextBody = ComposePlainText(body),
            HtmlBody = ComposeHtmlFromText(body),
            CorrelationId = $"{eventId}:organizer:{organizerUserId}"
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
            RecipientUserId = userId,
            RecipientAddressSource = RecipientAddressSource.TenantUserVerifiedEmail,
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
