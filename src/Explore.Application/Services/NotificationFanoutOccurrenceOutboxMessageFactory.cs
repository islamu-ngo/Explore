// ABOUTME: Creates and parses the PII-free general-outbox pointer for a fanout occurrence.
// ABOUTME: Uses source-generated JSON metadata so producer and worker share one durable contract.

using System.Text.Json;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Serialization;
using Explore.Domain;

namespace Explore.Application.Services;

public static class NotificationFanoutOccurrenceOutboxMessageFactory
{
    public const string EventType = "NotificationFanoutOccurrenceRequested";

    public static OutboxMessage Create(NotificationFanoutOccurrence occurrence)
    {
        var pointer = new NotificationFanoutOccurrenceRequested(
            occurrence.TenantId,
            occurrence.Id,
            NotificationFanoutOccurrenceRequested.CurrentVersion);

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateType = nameof(NotificationFanoutOccurrence),
            AggregateId = occurrence.Id,
            EventType = EventType,
            Payload = SerializePointer(pointer),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = occurrence.OccurredAt,
            MaxRetries = 5,
        };
    }

    public static string SerializePointer(NotificationFanoutOccurrenceRequested pointer)
        => JsonSerializer.Serialize(pointer, ExploreJsonContext.Default.NotificationFanoutOccurrenceRequested);

    public static NotificationFanoutOccurrenceRequested DeserializePointer(string json)
    {
        var pointer = JsonSerializer.Deserialize(
            json,
            ExploreJsonContext.Default.NotificationFanoutOccurrenceRequested)
            ?? throw new JsonException("Fanout occurrence pointer is required.");

        if (pointer.Version != NotificationFanoutOccurrenceRequested.CurrentVersion
            || pointer.TenantId == Guid.Empty
            || pointer.OccurrenceId == Guid.Empty)
        {
            throw new JsonException("Fanout occurrence pointer is invalid.");
        }

        return pointer;
    }
}
