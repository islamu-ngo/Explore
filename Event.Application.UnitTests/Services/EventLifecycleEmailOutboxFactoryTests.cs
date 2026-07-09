// ABOUTME: Unit tests for fixed Event lifecycle email automation outbox rows.
// ABOUTME: Verifies lifecycle actions create durable EmailDispatchOutbox intent without transport dependencies.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using ApplicationNotificationCategory = Explore.Application.Notifications.NotificationCategory;

namespace Event.Application.UnitTests.Services;

public sealed class EventLifecycleEmailOutboxFactoryTests
{
    private readonly CapturingNotificationOrchestrator _notificationOrchestrator = new();
    private readonly EventLifecycleEmailOutboxFactory _factory;

    public EventLifecycleEmailOutboxFactoryTests()
    {
        _factory = new EventLifecycleEmailOutboxFactory(_notificationOrchestrator);
    }

    [Test]
    public async Task CreateRegistrationApprovedUsesRegistrationIntentDedupKey()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationIntentId = Guid.NewGuid();

        var outbox = _factory.CreateRegistrationApproved(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            " attendee@example.test ",
            " Community Iftar ");

        await Assert.That(outbox.TenantId).IsEqualTo(tenantId);
        await Assert.That(outbox.Kind).IsEqualTo(EmailDispatchKind.RegistrationApproved);
        await Assert.That(outbox.SourceType).IsEqualTo(EventLifecycleEmailOutboxFactory.RegistrationIntentSourceType);
        await Assert.That(outbox.SourceId).IsEqualTo(registrationIntentId);
        await Assert.That(outbox.EventId).IsEqualTo(eventId);
        await Assert.That(outbox.RegistrationIntentId).IsEqualTo(registrationIntentId);
        await Assert.That(outbox.UserId).IsEqualTo(userId);
        await Assert.That(outbox.RecipientEmail).IsEqualTo("attendee@example.test");
        await Assert.That(outbox.Status).IsEqualTo(EmailDispatchStatus.Pending);
        await Assert.That(outbox.Subject).IsEqualTo("Registration approved for Community Iftar");
        await Assert.That(outbox.CorrelationId).IsEqualTo(registrationIntentId.ToString());
    }

    [Test]
    public async Task RegistrationLifecycleKindsUseDistinctEmailDispatchKindsForOutboxDeduplication()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationIntentId = Guid.NewGuid();

        var confirmation = _factory.CreateRegistrationConfirmation(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            "attendee@example.test",
            "Event");
        var rejected = _factory.CreateRegistrationRejected(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            "attendee@example.test",
            "Event");
        var promoted = _factory.CreateWaitlistPromoted(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            "attendee@example.test",
            "Event");
        var reminder = _factory.CreateEventReminder(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            "attendee@example.test",
            "Event",
            DateTimeOffset.UnixEpoch);
        var cancelled = _factory.CreateEventCancelled(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            "attendee@example.test",
            "Event");

        var kinds = new[]
        {
            confirmation.Kind,
            rejected.Kind,
            promoted.Kind,
            reminder.Kind,
            cancelled.Kind
        };

        await Assert.That(kinds.Distinct().Count()).IsEqualTo(kinds.Length);
        await Assert.That(kinds).Contains(EmailDispatchKind.RegistrationConfirmation);
        await Assert.That(kinds).Contains(EmailDispatchKind.RegistrationRejected);
        await Assert.That(kinds).Contains(EmailDispatchKind.WaitlistPromoted);
        await Assert.That(kinds).Contains(EmailDispatchKind.EventReminder);
        await Assert.That(kinds).Contains(EmailDispatchKind.EventCancelled);
    }

    [Test]
    public async Task CreateOrganizerNotificationUsesEventSourceKey()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var organizerUserId = Guid.NewGuid();

        var outbox = _factory.CreateOrganizerNotification(
            tenantId,
            eventId,
            organizerUserId,
            "organizer@example.test",
            "Fundraiser",
            "Capacity warning",
            "The event is almost full.");

        await Assert.That(outbox.TenantId).IsEqualTo(tenantId);
        await Assert.That(outbox.Kind).IsEqualTo(EmailDispatchKind.OrganizerNotification);
        await Assert.That(outbox.SourceType).IsEqualTo(EventLifecycleEmailOutboxFactory.EventSourceType);
        await Assert.That(outbox.SourceId).IsEqualTo(eventId);
        await Assert.That(outbox.EventId).IsEqualTo(eventId);
        await Assert.That(outbox.UserId).IsEqualTo(organizerUserId);
        await Assert.That(outbox.RegistrationIntentId).IsNull();
        await Assert.That(outbox.CorrelationId).IsEqualTo($"{eventId}:organizer:{organizerUserId}");
    }

    [Test]
    public async Task CreateRegistrationConfirmation_HtmlBodyEncodesEventTitle()
    {
        var outbox = _factory.CreateRegistrationConfirmation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "attendee@example.test",
            "<img src=x onerror=alert(1)> <script>alert(1)</script>");

        await Assert.That(outbox.HtmlBody).Contains("<strong>&lt;img src=x onerror=alert(1)&gt; &lt;script&gt;alert(1)&lt;/script&gt;</strong>");
        await Assert.That(outbox.HtmlBody).DoesNotContain("<img");
        await Assert.That(outbox.HtmlBody).DoesNotContain("<script");
    }

    [Test]
    public async Task CreateOrganizerNotification_HtmlBodyEncodesOrganizerBodyText()
    {
        var outbox = _factory.CreateOrganizerNotification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "organizer@example.test",
            "Fundraiser",
            "Capacity warning",
            "First line\r\n<script>alert(1)</script><img src=x onerror=alert(1)>");

        await Assert.That(outbox.PlainTextBody).Contains("<script>alert(1)</script>");
        await Assert.That(outbox.HtmlBody).Contains("First line<br />&lt;script&gt;alert(1)&lt;/script&gt;&lt;img src=x onerror=alert(1)&gt;");
        await Assert.That(outbox.HtmlBody).DoesNotContain("<script");
        await Assert.That(outbox.HtmlBody).DoesNotContain("<img");
        await Assert.That(outbox.HtmlBody).DoesNotContain("<img src=x onerror=");
    }

    [Test]
    public async Task EnqueueNotificationIntentAsync_RegistrationLifecycleUsesEmailOutboxDedupKey()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationIntentId = Guid.NewGuid();
        var outbox = _factory.CreateRegistrationApproved(
            tenantId,
            userId,
            eventId,
            registrationIntentId,
            "attendee@example.test",
            "Community Iftar");

        await _factory.EnqueueNotificationIntentAsync(outbox, CancellationToken.None);

        var draft = _notificationOrchestrator.LastDraft!;
        await Assert.That(draft.Category).IsEqualTo(ApplicationNotificationCategory.RegistrationLifecycle);
        await Assert.That(draft.TenantId).IsEqualTo(tenantId);
        await Assert.That(draft.RecipientKind).IsEqualTo("User");
        await Assert.That(draft.TemplateKey).IsEqualTo("registration.approved");
        await Assert.That(draft.SafePayloadReference).IsEqualTo($"event-registration-intent:{registrationIntentId}");
        await Assert.That(draft.DeduplicationKey).IsEqualTo($"event-registration-intent:{registrationIntentId}:registration-approved");
        await Assert.That(draft.CorrelationId).IsEqualTo(registrationIntentId.ToString());
        await Assert.That(draft.UserId).IsEqualTo(userId);
        await Assert.That(draft.EventId).IsEqualTo(eventId);
        await Assert.That(draft.IsUserFacing).IsTrue();
        await Assert.That(draft.IsIslamuInitiated).IsTrue();
    }

    [Test]
    public async Task EnqueueNotificationIntentAsync_OrganizerNotificationUsesEventLifecycleOwnership()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var organizerUserId = Guid.NewGuid();
        var outbox = _factory.CreateOrganizerNotification(
            tenantId,
            eventId,
            organizerUserId,
            "organizer@example.test",
            "Fundraiser",
            "Capacity warning",
            "The event is almost full.");

        await _factory.EnqueueNotificationIntentAsync(outbox, CancellationToken.None);

        var draft = _notificationOrchestrator.LastDraft!;
        await Assert.That(draft.Category).IsEqualTo(ApplicationNotificationCategory.EventLifecycle);
        await Assert.That(draft.TenantId).IsEqualTo(tenantId);
        await Assert.That(draft.RecipientKind).IsEqualTo("Organizer");
        await Assert.That(draft.TemplateKey).IsEqualTo("event.organizer.notification");
        await Assert.That(draft.SafePayloadReference).IsEqualTo($"event:{eventId}");
        await Assert.That(draft.DeduplicationKey).IsEqualTo($"event:{eventId}:event-organizer-notification");
        await Assert.That(draft.CorrelationId).IsEqualTo($"{eventId}:organizer:{organizerUserId}");
        await Assert.That(draft.UserId).IsEqualTo(organizerUserId);
        await Assert.That(draft.EventId).IsEqualTo(eventId);
    }

    private sealed class CapturingNotificationOrchestrator : INotificationOrchestrator
    {
        public NotificationIntentDraft? LastDraft { get; private set; }

        public Task<NotificationOrchestrationResult> EnqueueAsync(
            NotificationIntentDraft draft,
            CancellationToken cancellationToken = default)
        {
            LastDraft = draft;

            return Task.FromResult(new NotificationOrchestrationResult(
                new NotificationIntent
                {
                    TenantId = draft.TenantId ?? Guid.CreateVersion7(),
                    Tenant = null!,
                    CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
                    Category = null!,
                    OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
                    OwnershipType = null!,
                    RecipientKindId = (int)NotificationRecipientKindEnum.User,
                    RecipientKind = null!,
                    StatusId = (int)NotificationIntentStatusEnum.Pending,
                    Status = null!,
                    TemplateKey = draft.TemplateKey ?? string.Empty,
                    DeduplicationKey = draft.DeduplicationKey ?? string.Empty
                },
                new NotificationOwnershipDecision(draft.Category, NotificationOwnership.IslamuEvent)));
        }
    }
}
