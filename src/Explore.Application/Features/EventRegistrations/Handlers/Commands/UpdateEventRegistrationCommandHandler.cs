// ABOUTME: Handler for grouped EventRegistration PATCH updates with validation and concurrency.
// ABOUTME: Applies explicit groups atomically, saves once, and invalidates affected event caches.

using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Services.Registration;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventRegistrations.Handlers.Commands;

public class UpdateEventRegistrationCommandHandler : IRequestHandler<UpdateEventRegistrationCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IApprovalStatusRepository _approvalStatusRepository;
    private readonly IEventRegistrationIntentRepository _intentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IAtprotoRecordRepository _atprotoRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRegistrationNotificationDeliveryService _notificationDeliveryService;
    private readonly IRecipientNotificationMaterializer _recipientNotificationMaterializer;
    private readonly HybridCache _cache;

    public UpdateEventRegistrationCommandHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IUserRepository userRepository,
        IEventSessionRepository eventSessionRepository,
        IApprovalStatusRepository approvalStatusRepository,
        IEventRegistrationIntentRepository intentRepository,
        IEventRepository eventRepository,
        IAtprotoRecordRepository atprotoRecordRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRegistrationNotificationDeliveryService notificationDeliveryService,
        IRecipientNotificationMaterializer recipientNotificationMaterializer,
        HybridCache cache)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _userRepository = userRepository;
        _eventSessionRepository = eventSessionRepository;
        _approvalStatusRepository = approvalStatusRepository;
        _intentRepository = intentRepository;
        _eventRepository = eventRepository;
        _atprotoRecordRepository = atprotoRecordRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationDeliveryService = notificationDeliveryService;
        _recipientNotificationMaterializer = recipientNotificationMaterializer;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventRegistrationDtoValidator();
        var validationResult = await validator.ValidateAsync(request.EventRegistrationDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event Registration update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;
        var notificationIntentId = Guid.CreateVersion7();
        var emailDispatchOutboxId = Guid.CreateVersion7();
        var outcome = await _unitOfWork.ExecuteSerializableAsync(
            ct => ExecuteUpdateAsync(
                request,
                occurrenceId,
                occurredAt,
                notificationIntentId,
                emailDispatchOutboxId,
                ct),
            cancellationToken);

        if (outcome.Transition?.Changed == true)
        {
            await InvalidateCachesAsync(
                outcome.OldEventId,
                outcome.NewEventId,
                outcome.TenantId,
                cancellationToken);
        }

        return outcome.Response;
    }

    private async Task<UpdateExecutionOutcome> ExecuteUpdateAsync(
        UpdateEventRegistrationCommand request,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        Guid notificationIntentId,
        Guid emailDispatchOutboxId,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var eventRegistration = await _eventRegistrationRepository.GetById(request.EventRegistrationId);

        if (eventRegistration == null)
        {
            return new UpdateExecutionOutcome(
                new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = "Event Registration not found."
                });
        }

        if (eventRegistration.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event registration was modified by another request. Reload and retry.",
                nameof(EventRegistration),
                eventRegistration.Id.ToString());
        }

        var oldEventId = eventRegistration.EventId;
        var update = request.EventRegistrationDto;

        var effectiveUserId = eventRegistration.UserId;
        if (update.User is not null)
        {
            var user = await _userRepository.GetById(update.User.UserId);
            if (user is null)
            {
                return new UpdateExecutionOutcome(ValidationFailure("UserId not found."));
            }

            effectiveUserId = user.Id;
        }

        var effectiveSessionId = eventRegistration.EventSessionId;
        var effectiveEventId = eventRegistration.EventId;
        var effectiveTenantId = eventRegistration.TenantId;
        if (update.Session is not null)
        {
            var session = await _eventSessionRepository.GetById(update.Session.EventSessionId);
            if (session is null)
            {
                return new UpdateExecutionOutcome(ValidationFailure("EventSessionId not found."));
            }

            if (session.TenantId != eventRegistration.TenantId)
            {
                return new UpdateExecutionOutcome(ValidationFailure("EventSessionId must belong to the registration tenant."));
            }

            effectiveSessionId = session.Id;
            effectiveEventId = session.EventId;
            effectiveTenantId = session.TenantId;
        }

        var effectiveIntentId = eventRegistration.EventRegistrationIntentId;
        if (update.Intent?.EventRegistrationIntentId.HasValue == true)
        {
            effectiveIntentId = update.Intent.EventRegistrationIntentId.Value;
        }

        if (!RegistrationApprovalStatusRules.PreservesRegistrationIdentity(
                eventRegistration.EventRegistrationIntentId,
                effectiveIntentId,
                eventRegistration.UserId,
                effectiveUserId,
                eventRegistration.EventId,
                effectiveEventId,
                eventRegistration.TenantId,
                effectiveTenantId))
        {
            return new UpdateExecutionOutcome(ValidationFailure("Registration user, event, tenant, and parent intent are immutable."));
        }

        EventRegistrationIntent? registrationIntent = null;
        if (effectiveIntentId.HasValue)
        {
            registrationIntent = await _intentRepository.GetById(effectiveIntentId.Value);
            if (registrationIntent is null)
            {
                return new UpdateExecutionOutcome(ValidationFailure("EventRegistrationIntentId not found."));
            }

            if (registrationIntent.TenantId != effectiveTenantId || registrationIntent.EventId != effectiveEventId)
            {
                return new UpdateExecutionOutcome(ValidationFailure("EventRegistrationIntentId must belong to the effective registration event and tenant."));
            }
        }

        if (effectiveUserId != eventRegistration.UserId || effectiveSessionId != eventRegistration.EventSessionId)
        {
            var duplicate = await _eventRegistrationRepository.GetRegistrationByUserAndSession(effectiveUserId, effectiveSessionId, cancellationToken);
            if (duplicate is not null && duplicate.Id != eventRegistration.Id)
            {
                return new UpdateExecutionOutcome(ValidationFailure("A registration for the selected user and session already exists."));
            }
        }

        if (update.ApprovalStatus?.ApprovalStatusId.HasValue == true)
        {
            var desiredApprovalStatusId = update.ApprovalStatus.ApprovalStatusId.Value;
            if (!RegistrationApprovalStatusRules.CanTransition(
                    eventRegistration.ApprovalStatusId,
                    desiredApprovalStatusId))
            {
                return new UpdateExecutionOutcome(ValidationFailure("Terminal registration approval statuses cannot be changed."));
            }

            if (desiredApprovalStatusId.HasValue
                && !await _approvalStatusRepository.Exists(desiredApprovalStatusId.Value))
            {
                return new UpdateExecutionOutcome(ValidationFailure("ApprovalStatusId not found."));
            }
        }

        if (update.AtprotoRecord?.AtprotoRecordId.HasValue == true
            && update.AtprotoRecord.AtprotoRecordId.Value.HasValue
            && !await _atprotoRecordRepository.Exists(update.AtprotoRecord.AtprotoRecordId.Value.Value))
        {
            return new UpdateExecutionOutcome(ValidationFailure("AtprotoRecordId not found."));
        }

        ApplyUser(eventRegistration, update.User);
        ApplySession(eventRegistration, update.Session, effectiveEventId, effectiveTenantId);
        ApplyIntent(eventRegistration, update.Intent);
        ApplyApprovalStatus(eventRegistration, update.ApprovalStatus);
        ApplyAtprotoRecord(eventRegistration, update.AtprotoRecord);

        var actorUserId = _currentUserService.UserId;
        var actorProvenance = actorUserId switch
        {
            null => EventRegistrationActorProvenance.System,
            var id when id == eventRegistration.UserId => EventRegistrationActorProvenance.Attendee,
            _ => EventRegistrationActorProvenance.Organizer
        };
        var transition = await _eventRegistrationRepository.UpdateAndAdjustCapacityAsync(
            eventRegistration,
            occurrenceId,
            occurredAt,
            actorProvenance,
            actorUserId,
            cancellationToken);

        if (registrationIntent is not null
            && transition.Changed
            && transition.PreviousStatus != transition.FinalStatus)
        {
            var parentEvent = await _eventRepository.GetById(registrationIntent.EventId)
                ?? throw new InvalidOperationException("Registration lifecycle notification event was not found.");
            var recipient = await _userRepository.GetById(registrationIntent.UserId)
                ?? throw new InvalidOperationException("Registration lifecycle notification recipient was not found.");
            RecipientNotificationMaterialization? materialization =
                _notificationDeliveryService.CreateLifecycleMaterialization(
                    registrationIntent,
                    parentEvent.Title,
                    recipient,
                    transition,
                    notificationIntentId,
                    emailDispatchOutboxId);
            if (materialization is not null)
            {
                await _recipientNotificationMaterializer.MaterializeInCurrentTransactionAsync(
                    materialization,
                    cancellationToken);
            }
        }

        response.Success = true;
        response.Id = transition.ChildTransitions.LastOrDefault()?.RegistrationId ?? eventRegistration.Id;
        response.Message = "Event Registration updated successfully.";

        return new UpdateExecutionOutcome(
            response,
            transition,
            oldEventId,
            eventRegistration.EventId,
            eventRegistration.TenantId);
    }

    private static void ApplyUser(EventRegistration registration, UpdateEventRegistrationUserDto? group)
    {
        if (group is not null)
        {
            registration.UserId = group.UserId;
        }
    }

    private static void ApplySession(
        EventRegistration registration,
        UpdateEventRegistrationSessionDto? group,
        Guid effectiveEventId,
        Guid effectiveTenantId)
    {
        if (group is not null)
        {
            registration.EventSessionId = group.EventSessionId;
            registration.EventId = effectiveEventId;
            registration.TenantId = effectiveTenantId;
        }
    }

    private static void ApplyIntent(EventRegistration registration, UpdateEventRegistrationIntentDto? group)
    {
        if (group?.EventRegistrationIntentId.HasValue == true)
        {
            registration.EventRegistrationIntentId = group.EventRegistrationIntentId.Value;
        }
    }

    private static void ApplyApprovalStatus(EventRegistration registration, UpdateEventRegistrationApprovalStatusDto? group)
    {
        if (group?.ApprovalStatusId.HasValue == true)
        {
            registration.ApprovalStatusId = group.ApprovalStatusId.Value;
        }
    }

    private static void ApplyAtprotoRecord(EventRegistration registration, UpdateEventRegistrationAtprotoRecordDto? group)
    {
        if (group?.AtprotoRecordId.HasValue == true)
        {
            registration.AtprotoRecordId = group.AtprotoRecordId.Value;
        }
    }

    private async Task InvalidateCachesAsync(Guid oldEventId, Guid newEventId, Guid tenantId, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync($"event:detail:{newEventId}", cancellationToken);

        if (oldEventId != newEventId)
        {
            await _cache.RemoveAsync($"event:detail:{oldEventId}", cancellationToken);
        }

        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), cancellationToken);
    }

    private static BaseCommandResponse<Guid> ValidationFailure(string error)
    {
        return new BaseCommandResponse<Guid>
        {
            Success = false,
            Message = "Event Registration update failed.",
            Errors = [error]
        };
    }

    private sealed record UpdateExecutionOutcome(
        BaseCommandResponse<Guid> Response,
        EventRegistrationTransitionResult? Transition = null,
        Guid OldEventId = default,
        Guid NewEventId = default,
        Guid TenantId = default);
}
