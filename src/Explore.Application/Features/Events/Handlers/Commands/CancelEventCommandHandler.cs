// ABOUTME: Handler that transitions an event to the Cancelled lifecycle state.
// ABOUTME: Atomically persists the lifecycle change and durable attendee-notification occurrence.

using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public sealed class CancelEventCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache,
    IUserContext userContext,
    AtprotoEventPublicationPlanner atprotoPublicationPlanner,
    NotificationFanoutOccurrenceCoordinator fanoutCoordinator,
    IEventLifecycleScheduler eventLifecycleScheduler,
    TimeProvider timeProvider) : IRequestHandler<CancelEventCommand, BaseCommandResponse<Guid>>
{
    private const string ConcurrencyConflictCode = "event_cancel_concurrency_conflict";
    private const string AlreadyCancelledCode = "event_cancel_already_cancelled";
    private const string FanoutSourceType = "event_cancel_command";

    public async Task<BaseCommandResponse<Guid>> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var validator = new CancelEventRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(request.Id, "Event cancel request is invalid.", validationResult.Errors.Select(e => e.ErrorMessage));
        }

        Guid currentUserId = userContext.GetRequiredUserId();
        Guid federationOutboxId = Guid.CreateVersion7();
        Guid occurrenceId = Guid.CreateVersion7();
        Guid pointerOutboxMessageId = Guid.CreateVersion7();
        DateTime occurredAt = timeProvider.GetUtcNow().UtcDateTime;
        Guid? cancelledTenantId = null;

        BaseCommandResponse<Guid> response = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var @event = await eventRepository.GetById(request.Id);
            if (@event is null)
            {
                return Failure(request.Id, "Event was not found.", new[] { "Event was not found." });
            }

            if (@event.ConcurrencyStamp != request.Request.ExpectedConcurrencyStamp)
            {
                return Failure(request.Id, "Event was modified by another user.", new[] { "The event was modified by another user. Refresh and try again." }, ConcurrencyConflictCode);
            }

            if (@event.EventStatusId == (int)EventStatusEnum.Cancelled)
            {
                return Failure(request.Id, "Event is already cancelled.", new[] { "The event is already cancelled." }, AlreadyCancelledCode);
            }

            @event.EventStatusId = (int)EventStatusEnum.Cancelled;
            @event.UpdatedAt = occurredAt;

            await eventRepository.Update(@event);
            await atprotoPublicationPlanner.PlanEventAsync(
                new AtprotoEventPublicationInput(
                    @event.TenantId,
                    currentUserId,
                    @event.Id,
                    @event.ConcurrencyStamp,
                    PdsSyncOperation.Update,
                    federationOutboxId,
                    occurredAt),
                token);

            string snapshot = NotificationFanoutTemplateJson.Serialize(new NotificationFanoutSnapshotV1(
                @event.Title,
                SessionTitle: null,
                StartsAt: null,
                EndsAt: null,
                Timezone: null,
                Location: null));
            await fanoutCoordinator.CoordinateInCurrentTransactionAsync(
                new NotificationFanoutOccurrenceCandidate(
                    occurrenceId,
                    pointerOutboxMessageId,
                    @event.TenantId,
                    @event.Id,
                    SessionId: null,
                    occurredAt,
                    occurredAt,
                    request.Request.ExpectedConcurrencyStamp,
                    NotificationFanoutTemplateJson.Serialize(new NotificationFanoutChangeSetV1([
                        NotificationFanoutChangeField.Cancelled])),
                    snapshot,
                    snapshot,
                    NotificationFanoutRecipientTemplateFactory.EventCancelledTemplateKey,
                    NotificationFanoutRecipientTemplateFactory.CurrentTemplateVersion,
                    (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
                    NotificationFanoutRecipientTemplateFactory.CurrentPolicyVersion,
                    occurredAt,
                    FanoutSourceType,
                    @event.Id),
                token);
            await eventLifecycleScheduler.SuppressEventRemindersInCurrentTransactionAsync(
                new EventReminderSuppressionInput(
                    @event.TenantId,
                    @event.Id,
                    RegistrationOrderId: null,
                    SessionId: null,
                    occurredAt,
                    "event_cancelled"),
                token);
            cancelledTenantId = @event.TenantId;

            return Success(@event.Id, "Event cancelled successfully.");
        }, cancellationToken);

        if (!response.Success || !cancelledTenantId.HasValue)
        {
            return response;
        }

        await cache.RemoveAsync($"event:detail:{request.Id}", cancellationToken);
        await cache.RemoveByTagAsync(CacheTags.EventListByTenant(cancelledTenantId.Value), cancellationToken);
        return response;
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(Guid id, string message, IEnumerable<string> errors, string? failureCode = null) => new()
    {
        Success = false,
        Id = id,
        Message = message,
        Errors = errors.ToList(),
        FailureCode = failureCode
    };
}
