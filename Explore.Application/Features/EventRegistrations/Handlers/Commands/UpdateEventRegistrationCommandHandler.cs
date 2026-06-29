// ABOUTME: Handler for grouped EventRegistration PATCH updates with validation and concurrency.
// ABOUTME: Applies explicit groups atomically, saves once, and invalidates affected event caches.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.DTOs.EventRegistration.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
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
    private readonly IAtprotoRecordRepository _atprotoRecordRepository;
    private readonly HybridCache _cache;

    public UpdateEventRegistrationCommandHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IUserRepository userRepository,
        IEventSessionRepository eventSessionRepository,
        IApprovalStatusRepository approvalStatusRepository,
        IEventRegistrationIntentRepository intentRepository,
        IAtprotoRecordRepository atprotoRecordRepository,
        HybridCache cache)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _userRepository = userRepository;
        _eventSessionRepository = eventSessionRepository;
        _approvalStatusRepository = approvalStatusRepository;
        _intentRepository = intentRepository;
        _atprotoRecordRepository = atprotoRecordRepository;
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

        var eventRegistration = await _eventRegistrationRepository.GetById(request.EventRegistrationId);

        if (eventRegistration == null)
        {
            response.Success = false;
            response.Message = "Event Registration not found.";
            return response;
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
                return ValidationFailure("UserId not found.");
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
                return ValidationFailure("EventSessionId not found.");
            }

            if (session.TenantId != eventRegistration.TenantId)
            {
                return ValidationFailure("EventSessionId must belong to the registration tenant.");
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

        if (effectiveIntentId.HasValue)
        {
            var intent = await _intentRepository.GetById(effectiveIntentId.Value);
            if (intent is null)
            {
                return ValidationFailure("EventRegistrationIntentId not found.");
            }

            if (intent.TenantId != effectiveTenantId || intent.EventId != effectiveEventId)
            {
                return ValidationFailure("EventRegistrationIntentId must belong to the effective registration event and tenant.");
            }
        }

        if (effectiveUserId != eventRegistration.UserId || effectiveSessionId != eventRegistration.EventSessionId)
        {
            var duplicate = await _eventRegistrationRepository.GetRegistrationByUserAndSession(effectiveUserId, effectiveSessionId);
            if (duplicate is not null && duplicate.Id != eventRegistration.Id)
            {
                return ValidationFailure("A registration for the selected user and session already exists.");
            }
        }

        if (update.ApprovalStatus?.ApprovalStatusId.HasValue == true
            && update.ApprovalStatus.ApprovalStatusId.Value.HasValue
            && !await _approvalStatusRepository.Exists(update.ApprovalStatus.ApprovalStatusId.Value.Value))
        {
            return ValidationFailure("ApprovalStatusId not found.");
        }

        if (update.AtprotoRecord?.AtprotoRecordId.HasValue == true
            && update.AtprotoRecord.AtprotoRecordId.Value.HasValue
            && !await _atprotoRecordRepository.Exists(update.AtprotoRecord.AtprotoRecordId.Value.Value))
        {
            return ValidationFailure("AtprotoRecordId not found.");
        }

        ApplyUser(eventRegistration, update.User);
        ApplySession(eventRegistration, update.Session, effectiveEventId, effectiveTenantId);
        ApplyIntent(eventRegistration, update.Intent);
        ApplyApprovalStatus(eventRegistration, update.ApprovalStatus);
        ApplyAtprotoRecord(eventRegistration, update.AtprotoRecord);

        await _eventRegistrationRepository.Update(eventRegistration);
        await InvalidateCachesAsync(oldEventId, eventRegistration.EventId, eventRegistration.TenantId, cancellationToken);

        response.Success = true;
        response.Id = eventRegistration.Id;
        response.Message = "Event Registration updated successfully.";

        return response;
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
}
