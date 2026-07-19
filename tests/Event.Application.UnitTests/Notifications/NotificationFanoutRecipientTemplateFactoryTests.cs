// ABOUTME: Verifies the closed fanout template set and immutable recipient rendering.
// ABOUTME: Covers strict contract drift, occurrence linkage, and snapshot-only location disclosure.

using System.Collections.Immutable;
using System.Text.Json;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Event.Application.UnitTests.Notifications;

public sealed class NotificationFanoutRecipientTemplateFactoryTests
{
    private readonly NotificationFanoutRecipientTemplateFactory factory = new();

    [Test]
    [Arguments(NotificationFanoutRecipientTemplateFactory.EventCancelledTemplateKey, false, true)]
    [Arguments(NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey, false, false)]
    [Arguments(NotificationFanoutRecipientTemplateFactory.SessionCancelledTemplateKey, true, true)]
    [Arguments(NotificationFanoutRecipientTemplateFactory.SessionUpdatedTemplateKey, true, false)]
    public async Task VersionOneTemplateCreatesOccurrenceLinkedConfiguredChannels(
        string templateKey,
        bool sessionScoped,
        bool cancelled)
    {
        NotificationFanoutOccurrence occurrence = CreateOccurrence(templateKey, sessionScoped, cancelled);
        Guid recipientUserId = Guid.CreateVersion7();
        NotificationFanoutRecipientTemplate template = factory.Parse(occurrence);

        var request = factory.CreateMaterialization(
            occurrence,
            template,
            recipientUserId,
            "current-verified@example.test",
            emailPreferenceEnabled: true,
            emailSkipReason: null,
            locationAuthorization: null);

        await Assert.That(request.Intent.FanoutOccurrenceId).IsEqualTo(occurrence.Id);
        await Assert.That(request.Intent.DeduplicationKey).Contains(occurrence.Id.ToString("N"));
        await Assert.That(request.InApp).IsNotNull();
        await Assert.That(request.Email).IsNotNull();
        await Assert.That(request.Email!.SourceType).IsEqualTo(NotificationFanoutRecipientTemplateFactory.OccurrenceSourceType);
        await Assert.That(request.Email.SourceId).IsEqualTo(occurrence.Id);
        await Assert.That(request.Email.Kind).IsEqualTo(cancelled ? EmailDispatchKind.EventCancelled : EmailDispatchKind.EventUpdated);
    }

    [Test]
    public async Task UnknownVersionPolicyKeyAndScopeFailClosed()
    {
        NotificationFanoutOccurrence unknownKey = CreateOccurrence("event.reminder", false, false);
        NotificationFanoutOccurrence unknownVersion = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false,
            templateVersion: 2);
        NotificationFanoutOccurrence wrongPolicy = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false,
            deliveryPolicyId: (int)NotificationDeliveryPolicyEnum.ReminderOptional);
        NotificationFanoutOccurrence wrongScope = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.SessionUpdatedTemplateKey,
            false,
            false);

        await Assert.ThrowsAsync<JsonException>(() => Task.FromResult(factory.Parse(unknownKey)));
        await Assert.ThrowsAsync<JsonException>(() => Task.FromResult(factory.Parse(unknownVersion)));
        await Assert.ThrowsAsync<JsonException>(() => Task.FromResult(factory.Parse(wrongPolicy)));
        await Assert.ThrowsAsync<JsonException>(() => Task.FromResult(factory.Parse(wrongScope)));
    }

    [Test]
    public async Task MalformedAndUnknownSnapshotMembersFailClosed()
    {
        NotificationFanoutOccurrence malformed = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false,
            afterJson: "{");
        string valid = SnapshotJson(sessionScoped: false, "Immutable event", "Immutable venue");
        NotificationFanoutOccurrence extraMember = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false,
            afterJson: valid[..^1] + ",\"recipientEmail\":\"forbidden@example.test\"}");

        await Assert.ThrowsAsync<JsonException>(() => Task.FromResult(factory.Parse(malformed)));
        await Assert.ThrowsAsync<JsonException>(() => Task.FromResult(factory.Parse(extraMember)));
    }

    [Test]
    public async Task LocationMaskSelectsOnlyImmutableSnapshotValues()
    {
        NotificationFanoutOccurrence occurrence = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false);
        NotificationFanoutRecipientTemplate template = factory.Parse(occurrence);
        Guid recipientUserId = Guid.CreateVersion7();
        var authorization = new FanoutAttendeeLocationAuthorizationResult(
            occurrence.TenantId,
            occurrence.EventId,
            recipientUserId,
            template.After.Location!.EventLocationId,
            template.After.Location.RoomId,
            EventLocationDisclosureState.Available,
            ImmutableArray.Create(EventLocationDisclosureField.VenueName));

        var request = factory.CreateMaterialization(
            occurrence,
            template,
            recipientUserId,
            "verified@example.test",
            emailPreferenceEnabled: true,
            emailSkipReason: null,
            authorization);

        await Assert.That(request.Email!.PlainTextBody).Contains("Immutable venue");
        await Assert.That(request.Email.PlainTextBody).DoesNotContain("current database venue");
        await Assert.That(typeof(FanoutAttendeeLocationAuthorizationResult).GetProperties()
            .Any(property => property.Name is "Values" or "VenueName" or "StreetAddress")).IsFalse();
    }

    [Test]
    public async Task LocationAuthorizationForAnotherRecipientFailsClosed()
    {
        NotificationFanoutOccurrence occurrence = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false);
        NotificationFanoutRecipientTemplate template = factory.Parse(occurrence);
        Guid recipientUserId = Guid.CreateVersion7();
        var authorization = CreateAuthorization(
            occurrence,
            template.After.Location!,
            Guid.CreateVersion7());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(factory.CreateMaterialization(
            occurrence,
            template,
            recipientUserId,
            "verified@example.test",
            emailPreferenceEnabled: true,
            emailSkipReason: null,
            authorization)));
    }

    [Test]
    public async Task LocationAuthorizationForAnotherRoomFailsClosed()
    {
        NotificationFanoutOccurrence occurrence = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false);
        NotificationFanoutRecipientTemplate template = factory.Parse(occurrence);
        Guid recipientUserId = Guid.CreateVersion7();
        FanoutAttendeeLocationAuthorizationResult authorization = CreateAuthorization(
            occurrence,
            template.After.Location!,
            recipientUserId) with
        {
            RoomId = Guid.CreateVersion7()
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(factory.CreateMaterialization(
            occurrence,
            template,
            recipientUserId,
            "verified@example.test",
            emailPreferenceEnabled: true,
            emailSkipReason: null,
            authorization)));
    }

    [Test]
    [Arguments("{\"fields\":[\"Title\"]}")]
    [Arguments("{\"fields\":[2]}")]
    public async Task FormerTitleChangeFieldFailsClosed(string changeSetJson)
    {
        NotificationFanoutOccurrence occurrence = CreateOccurrence(
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            false,
            false,
            changeSetJson: changeSetJson);

        await Assert.ThrowsAsync<JsonException>(() => Task.FromResult(factory.Parse(occurrence)));
    }

    private static NotificationFanoutOccurrence CreateOccurrence(
        string templateKey,
        bool sessionScoped,
        bool cancelled,
        int templateVersion = 1,
        int deliveryPolicyId = (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
        string? afterJson = null,
        string? changeSetJson = null)
    {
        DateTime now = DateTime.UtcNow;
        return NotificationFanoutOccurrence.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            sessionScoped ? Guid.CreateVersion7() : null,
            now,
            now,
            Guid.CreateVersion7(),
            changeSetJson ?? NotificationFanoutTemplateJson.Serialize(new NotificationFanoutChangeSetV1(
                cancelled
                    ? [NotificationFanoutChangeField.Cancelled]
                    : [NotificationFanoutChangeField.StartTime, NotificationFanoutChangeField.Location])),
            SnapshotJson(sessionScoped, "Immutable event", "Old immutable venue"),
            afterJson ?? SnapshotJson(sessionScoped, "Immutable event", "Immutable venue"),
            templateKey,
            templateVersion,
            deliveryPolicyId,
            NotificationFanoutRecipientTemplateFactory.CurrentPolicyVersion,
            30,
            now,
            sessionScoped ? "event-session" : "event",
            Guid.CreateVersion7(),
            $"fanout:{Guid.NewGuid():N}",
            null);
    }

    private static FanoutAttendeeLocationAuthorizationResult CreateAuthorization(
        NotificationFanoutOccurrence occurrence,
        NotificationFanoutLocationSnapshotV1 location,
        Guid recipientUserId) => new(
            occurrence.TenantId,
            occurrence.EventId,
            recipientUserId,
            location.EventLocationId,
            location.RoomId,
            EventLocationDisclosureState.Available,
            ImmutableArray.Create(EventLocationDisclosureField.VenueName));

    private static string SnapshotJson(bool sessionScoped, string eventTitle, string venueName) =>
        NotificationFanoutTemplateJson.Serialize(new NotificationFanoutSnapshotV1(
            eventTitle,
            sessionScoped ? "Immutable session" : null,
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.FromHours(2)),
            "Europe/Brussels",
            new NotificationFanoutLocationSnapshotV1(
                Guid.Parse("0198a111-1111-7111-8111-111111111111"),
                null,
                "Belgium",
                "Brussels",
                venueName,
                null,
                "Immutable street 1",
                "1000")));
}
