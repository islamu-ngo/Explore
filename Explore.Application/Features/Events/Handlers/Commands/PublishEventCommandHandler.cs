using System.Text.Json;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Models.IntegrationEvents;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class PublishEventCommandHandler(
    IEventRepository eventRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<PublishEventCommand, BaseCommandResponse<Guid>>
{
    private const string ConcurrencyConflictCode = "event_publish_concurrency_conflict";
    private const string ReadinessFailedCode = "event_publish_readiness_failed";
    private const string EventAggregateType = "Event";
    public const string EventPublishedEventType = "EventPublished";
    public const string EventPublishedNotificationFanoutRequestedEventType = "EventPublishedNotificationFanoutRequested";

    public async Task<BaseCommandResponse<Guid>> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var validator = new PublishEventRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(request.Id, "Event publish request is invalid.", validationResult.Errors.Select(error => error.ErrorMessage));
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var @event = await eventRepository.GetById(request.Id);
            if (@event is null)
                return Failure(request.Id, "Event not found.", ["Event not found."]);

            if (@event.ConcurrencyStamp != request.Request.ExpectedConcurrencyStamp)
            {
                return Failure(
                    request.Id,
                    "Event was changed by another request.",
                    ["Refresh the event and try publishing again."],
                    ConcurrencyConflictCode);
            }

            var readiness = EventPublishReadinessEvaluator.Evaluate(@event);
            if (!readiness.IsReady)
                return Failure(request.Id, "Event is not ready to publish.", readiness.Errors.Select(error => error.Message), ReadinessFailedCode);

            if (@event.EventStatusId == (int)EventStatusEnum.Published)
                return Success(@event.Id, "Event is already published.");

            @event.EventStatusId = (int)EventStatusEnum.Published;
            @event.UpdatedAt = DateTime.UtcNow;

            await eventRepository.Update(@event);

            var publishedAt = DateTimeOffset.UtcNow;
            await outboxRepository.Create(CreatePublishedOutboxMessage(@event));
            await outboxRepository.Create(CreateNotificationFanoutOutboxMessage(@event, publishedAt));

            await cache.RemoveAsync($"event:detail:{@event.Id}", token);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), token);

            return Success(@event.Id, "Event published successfully.");
        }, cancellationToken);
    }

    private static OutboxMessage CreatePublishedOutboxMessage(Event @event)
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

    private static OutboxMessage CreateNotificationFanoutOutboxMessage(Event @event, DateTimeOffset publishedAt)
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

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(
        Guid id,
        string message,
        IEnumerable<string> errors,
        string? failureCode = null) => new()
    {
        Success = false,
        Id = id,
        Message = message,
        Errors = errors.ToList(),
        FailureCode = failureCode
    };
}
