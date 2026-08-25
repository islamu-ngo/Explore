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
using Explore.Domain.Services.Lifecycle;
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
    AtprotoEventPublicationPlanner atprotoPublicationPlanner,
    TimeProvider timeProvider) : IRequestHandler<PublishEventCommand, BaseCommandResponse<Guid>>
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

        var @event = await eventRepository.GetById(request.Id);
        if (@event is null)
            return Failure(request.Id, "Event not found.", ["Event not found."]);

        EventStatusEnum currentStatus = (EventStatusEnum)@event.EventStatusId;
        if (currentStatus == EventStatusEnum.Published)
            return Success(@event.Id, "Event is already published.");

        if (@event.ConcurrencyStamp != request.Request.ExpectedConcurrencyStamp)
        {
            return Failure(
                request.Id,
                "Event was changed by another request.",
                ["Refresh the event and try publishing again."],
                ConcurrencyConflictCode);
        }

        if (!EventLifecycleRules.CanTransition(currentStatus, EventStatusEnum.Published))
        {
            return Failure(request.Id, "Event is not ready to publish.", [PublishTransitionReadinessError(currentStatus)], ReadinessFailedCode);
        }

        var currentUserId = userContext.GetRequiredUserId();
        var federationOutboxId = Guid.CreateVersion7();
        var notificationFanoutOutboxId = Guid.CreateVersion7();
        DateTime occurredAt = timeProvider.GetUtcNow().UtcDateTime;
        var federationCreatedAt = occurredAt;
        Guid? tenantIdToInvalidate = null;
        bool mutationAttempted = false;

        BaseCommandResponse<Guid> response = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            tenantIdToInvalidate = null;
            var attemptEvent = await eventRepository.GetById(request.Id);
            if (attemptEvent is null)
                return Failure(request.Id, "Event not found.", ["Event not found."]);

            EventStatusEnum attemptStatus = (EventStatusEnum)attemptEvent.EventStatusId;
            if (mutationAttempted && attemptStatus == EventStatusEnum.Published)
            {
                tenantIdToInvalidate = attemptEvent.TenantId;
                return Success(attemptEvent.Id, "Event published successfully.");
            }

            if (attemptEvent.ConcurrencyStamp != request.Request.ExpectedConcurrencyStamp)
            {
                return Failure(
                    request.Id,
                    "Event was changed by another request.",
                    ["Refresh the event and try publishing again."],
                    ConcurrencyConflictCode);
            }

            if (!EventLifecycleRules.CanTransition(attemptStatus, EventStatusEnum.Published))
                return Failure(request.Id, "Event is not ready to publish.", [PublishTransitionReadinessError(attemptStatus)], ReadinessFailedCode);

            EventLifecyclePolicy policy = await policyProvider.GetEffectivePolicyAsync(attemptEvent.TenantId, ValidationProfile.EventPublish, token);
            LifecycleReadinessResult readiness = readinessEvaluator.Evaluate(attemptEvent, policy.Profile, policy);
            IReadOnlyList<EventLocation> eventLocations = await eventLocationRepository.GetByEventIdAsync(
                attemptEvent.Id,
                token);
            readiness = EventLocationPublicationReadinessEvaluator.Include(readiness, eventLocations);
            if (!readiness.IsReady)
                return Failure(request.Id, "Event is not ready to publish.", readiness.Errors.Select(error => error.Message), ReadinessFailedCode);

            attemptEvent.Publish(occurredAt);
            mutationAttempted = true;

            await eventRepository.Update(attemptEvent);

            await atprotoPublicationPlanner.PlanEventAsync(
                new AtprotoEventPublicationInput(
                    attemptEvent.TenantId,
                    currentUserId,
                    attemptEvent.Id,
                    attemptEvent.ConcurrencyStamp,
                    PdsSyncOperation.Create,
                    federationOutboxId,
                    federationCreatedAt),
                token);

            var publishedAt = new DateTimeOffset(occurredAt, TimeSpan.Zero);
            await outboxRepository.Create(EventPublishedOutboxMessageFactory.CreateNotificationFanoutOutboxMessage(
                notificationFanoutOutboxId,
                attemptEvent,
                publishedAt));

            tenantIdToInvalidate = attemptEvent.TenantId;
            return Success(attemptEvent.Id, "Event published successfully.");
        }, cancellationToken);

        if (response.IsSuccess && tenantIdToInvalidate.HasValue)
        {
            await cache.RemoveAsync($"event:detail:{request.Id}", cancellationToken);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(tenantIdToInvalidate.Value), cancellationToken);
        }

        return response;
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) =>
        BaseCommandResponse.Success(id, message);

    private static BaseCommandResponse<Guid> Failure(
        Guid id,
        string message,
        IEnumerable<string> errors,
        string? failureCode = null) => failureCode is null
            ? BaseCommandResponse.Validation(errors, message, id)
            : BaseCommandResponse.Failure(failureCode, message, errors, id);

    private static string PublishTransitionReadinessError(EventStatusEnum status) => status switch
    {
        EventStatusEnum.Cancelled => "Event is cancelled and cannot be published.",
        EventStatusEnum.Moderated => "Event is moderated and cannot be published.",
        EventStatusEnum.Archived => "Event is archived and cannot be published.",
        _ => "Event cannot be published from its current status."
    };
}
