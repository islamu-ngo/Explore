// ABOUTME: Handler for publishing an existing draft Event.
// ABOUTME: Validates concurrency stamp and policy-aware publish readiness, transitions status to Published, and creates outbox messages.

using System.Text.Json;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class PublishEventCommandHandler(
    IEventRepository eventRepository,
    IEventLocationRepository eventLocationRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache,
    IEventLifecyclePolicyProvider policyProvider,
    IEventLifecycleReadinessEvaluator readinessEvaluator,
    IUserContext userContext,
    AtprotoEventPublicationPlanner atprotoPublicationPlanner) : IRequestHandler<PublishEventCommand, BaseCommandResponse<Guid>>
{
    private const string ConcurrencyConflictCode = "event_publish_concurrency_conflict";
    private const string ReadinessFailedCode = "event_publish_readiness_failed";
    private const string EventAggregateType = "Event";
    public const string EventPublishedNotificationFanoutRequestedEventType = "EventPublishedNotificationFanoutRequested";

    public async Task<BaseCommandResponse<Guid>> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var validator = new PublishEventRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(request.Id, "Event publish request is invalid.", validationResult.Errors.Select(error => error.ErrorMessage));
        }

        var currentUserId = userContext.GetRequiredUserId();
        var federationOutboxId = Guid.CreateVersion7();
        var federationCreatedAt = DateTime.UtcNow;

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

            EventLifecyclePolicy policy = await policyProvider.GetEffectivePolicyAsync(@event.TenantId, ValidationProfile.EventPublish, token);
            LifecycleReadinessResult readiness = readinessEvaluator.Evaluate(@event, policy.Profile, policy);
            IReadOnlyList<EventLocation> eventLocations = await eventLocationRepository.GetByEventIdAsync(
                @event.Id,
                token);
            readiness = EventLocationPublicationReadinessEvaluator.Include(readiness, eventLocations);
            if (!readiness.IsReady)
                return Failure(request.Id, "Event is not ready to publish.", readiness.Errors.Select(error => error.Message), ReadinessFailedCode);

            if (@event.EventStatusId == (int)EventStatusEnum.Published)
                return Success(@event.Id, "Event is already published.");

            @event.EventStatusId = (int)EventStatusEnum.Published;
            @event.UpdatedAt = DateTime.UtcNow;

            await eventRepository.Update(@event);

            await atprotoPublicationPlanner.PlanEventAsync(
                new AtprotoEventPublicationInput(
                    @event.TenantId,
                    currentUserId,
                    @event.Id,
                    @event.ConcurrencyStamp,
                    PdsSyncOperation.Create,
                    federationOutboxId,
                    federationCreatedAt),
                token);

            var publishedAt = DateTimeOffset.UtcNow;
            await outboxRepository.Create(EventPublishedOutboxMessageFactory.CreateNotificationFanoutOutboxMessage(@event, publishedAt));

            await cache.RemoveAsync($"event:detail:{@event.Id}", token);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), token);

            return Success(@event.Id, "Event published successfully.");
        }, cancellationToken);
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
