// ABOUTME: Verifies the fanout general-outbox pointer is source-generated and PII-free.
// ABOUTME: Locks the pointer contract to tenant, occurrence, and schema-version identifiers only.

using System.Text.Json;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;

namespace Event.Application.UnitTests.Notifications;

public sealed class NotificationFanoutOccurrencePointerTests
{
    [Test]
    public async Task Serialize_RoundTripsOnlyDurableIdentifiers()
    {
        var pointer = new NotificationFanoutOccurrenceRequested(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationFanoutOccurrenceRequested.CurrentVersion);

        string json = NotificationFanoutOccurrenceOutboxMessageFactory.SerializePointer(pointer);
        var roundTrip = NotificationFanoutOccurrenceOutboxMessageFactory.DeserializePointer(json);

        await Assert.That(roundTrip).IsEqualTo(pointer);
        using var document = JsonDocument.Parse(json);
        string[] propertyNames = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
        await Assert.That(propertyNames).IsEquivalentTo(["tenantId", "occurrenceId", "version"]);
        await Assert.That(json).DoesNotContain("recipient", StringComparison.OrdinalIgnoreCase);
        await Assert.That(json).DoesNotContain("email", StringComparison.OrdinalIgnoreCase);
        await Assert.That(json).DoesNotContain("body", StringComparison.OrdinalIgnoreCase);
        await Assert.That(json).DoesNotContain("title", StringComparison.OrdinalIgnoreCase);
        await Assert.That(json).DoesNotContain("location", StringComparison.OrdinalIgnoreCase);
        await Assert.That(json).DoesNotContain("evidence", StringComparison.OrdinalIgnoreCase);
    }
}
