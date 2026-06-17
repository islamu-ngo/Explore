// ABOUTME: Shared factory for event publish outbox messages.
// ABOUTME: Generates integration and internal notification fanout outbox messages to avoid duplication between handlers.

using System;
using System.Text.Json;
using Explore.Application.Models.IntegrationEvents;
using Explore.Application.Models.InternalEvents;
using Explore.Domain;

namespace Explore.Application.Services;

public static class EventPublishedOutboxMessageFactory
{
    private const string EventAggregateType = "Event";
    public const string EventPublishedEventType = "EventPublished";
    public const string EventPublishedNotificationFanoutRequestedEventType = "EventPublishedNotificationFanoutRequested";

    public static OutboxMessage CreatePublishedOutboxMessage(Event @event)
    {
        var payload = new EventPublishedIntegrationEvent
        {
            TenantId = @event.TenantId,
            EventId = @event.Id,
            Title = @event.Title,
            StartDate = @event.FirstSessionStartUtc!.Value,
            EndDate = @event.LastSessionStartUtc,
            IsDeleted = false
        };

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = EventAggregateType,
            AggregateId = @event.Id,
            EventType = EventPublishedEventType,
            Payload = JsonSerializer.Serialize(payload),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            MaxRetries = 5
        };
    }

    public static OutboxMessage CreateNotificationFanoutOutboxMessage(Event @event, DateTimeOffset publishedAt)
    {
        var payload = new EventPublishedNotificationFanoutRequested
        {
            TenantId = @event.TenantId,
            EventId = @event.Id,
            EventTitle = @event.Title,
            SourceActorId = @event.ActorId,
            StartDate = @event.FirstSessionStartUtc!.Value,
            EndDate = @event.LastSessionStartUtc,
            PublishedAt = publishedAt
        };

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = EventAggregateType,
            AggregateId = @event.Id,
            EventType = EventPublishedNotificationFanoutRequestedEventType,
            Payload = JsonSerializer.Serialize(payload),
            Status = OutboxMessageStatus.Pending,
            CreatedAt = publishedAt.UtcDateTime,
            MaxRetries = 5
        };
    }
}
