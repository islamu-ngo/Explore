// ABOUTME: Applies local draft-event workflow updates without AutoMapper broad-field ownership leaks.
// ABOUTME: Preserves status, actor, tenant, and session-derived projection fields as server-owned state.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public sealed class UpdateEventDraftCommandHandler : IRequestHandler<UpdateEventDraftCommand, BaseCommandResponse<Guid>>
{
    public const string ConcurrencyConflictCode = "event_draft_concurrency_conflict";

    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipationConfigurationRepository _participationConfigurationRepository;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IVisibilityTypeRepository _visibilityTypeRepository;
    private readonly IEventFormatRepository _eventFormatRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IEventRegistrationPolicyRepository _eventRegistrationPolicyRepository;
    private readonly IEventScheduleProjectionCalculator _scheduleProjectionCalculator;
    private readonly HybridCache _cache;

    public UpdateEventDraftCommandHandler(
        IEventRepository eventRepository,
        IEventParticipationConfigurationRepository participationConfigurationRepository,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IVisibilityTypeRepository visibilityTypeRepository,
        IEventFormatRepository eventFormatRepository,
        IStorageObjectRepository storageObjectRepository,
        IEventSeriesRepository eventSeriesRepository,
        IEventRegistrationPolicyRepository eventRegistrationPolicyRepository,
        IEventScheduleProjectionCalculator scheduleProjectionCalculator,
        HybridCache cache)
    {
        _eventRepository = eventRepository;
        _participationConfigurationRepository = participationConfigurationRepository;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _visibilityTypeRepository = visibilityTypeRepository;
        _eventFormatRepository = eventFormatRepository;
        _storageObjectRepository = storageObjectRepository;
        _eventSeriesRepository = eventSeriesRepository;
        _eventRegistrationPolicyRepository = eventRegistrationPolicyRepository;
        _scheduleProjectionCalculator = scheduleProjectionCalculator;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventDraftCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var validator = new UpdateEventDraftRequestDtoValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _visibilityTypeRepository,
            _eventFormatRepository,
            _storageObjectRepository,
            _eventSeriesRepository,
            _eventRegistrationPolicyRepository);

        var validationResult = await validator.ValidateAsync(request.Draft, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event draft update failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var eventEntity = await _eventRepository.GetScheduleGraphForUpdateAsync(request.Id, cancellationToken);
        if (eventEntity is null)
        {
            response.Success = false;
            response.Message = "Event not found.";
            return response;
        }

        if (eventEntity.ConcurrencyStamp != request.Draft.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event draft changed since it was loaded. Refresh the event and try again.",
                "event",
                eventEntity.Id.ToString());
        }

        if (!await ImageReferenceEligibility.AreEligibleAsync(
                _storageObjectRepository,
                eventEntity.TenantId,
                request.Draft.FeaturedImageId,
                request.Draft.BackgroundImageId))
        {
            response.Success = false;
            response.Message = "Event draft update failed.";
            response.Errors = ["Every image must be an active public safe-raster object in the current tenant."];
            return response;
        }

        EventParticipationConfiguration? participationConfiguration =
            await _participationConfigurationRepository.GetByEventAndTenantAsync(
                eventEntity.Id,
                eventEntity.TenantId,
                cancellationToken);
        if (participationConfiguration is null)
        {
            response.Success = false;
            response.Message = "Event participation configuration not found.";
            response.FailureCode = "event_participation_configuration_not_found";
            return response;
        }

        if (participationConfiguration.ConcurrencyStamp
            != request.Draft.ExpectedParticipationConfigurationConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event participation configuration changed since it was loaded. Refresh the event and try again.",
                "event_participation_configuration",
                eventEntity.Id.ToString());
        }

        participationConfiguration.Reconfigure(
            request.Draft.ParticipationConfiguration.ParticipationHandlingModeId,
            request.Draft.ParticipationConfiguration.AdvanceRegistrationObligationId,
            request.Draft.ParticipationConfiguration.IdentityAccessModeId,
            request.Draft.ParticipationConfiguration.GuestRecoveryPolicy);

        var draft = request.Draft;
        eventEntity.Title = draft.Title;
        eventEntity.Subtitle = draft.Subtitle;
        eventEntity.Description = draft.Description;
        eventEntity.Content = draft.Content;
        eventEntity.Slug = draft.Slug;
        eventEntity.EventTypeId = draft.EventTypeId;
        eventEntity.AudienceGenderId = draft.AudienceGenderId;
        eventEntity.AudienceAgeId = draft.AudienceAgeId;
        eventEntity.Price = draft.Price;
        eventEntity.CurrencyCode = draft.CurrencyCode;
        eventEntity.FeaturedImageId = draft.FeaturedImageId;
        eventEntity.VisibilityTypeId = draft.VisibilityTypeId;
        eventEntity.EventFormatId = draft.EventFormatId;
        eventEntity.MadhabId = draft.MadhabId;
        var timezoneId = ScheduleTimeZoneResolver.NormalizeOrUtc(draft.EventTimeZoneId ?? draft.Timezone);
        eventEntity.Timezone = timezoneId;
        eventEntity.EventTimeZoneId = timezoneId;
        eventEntity.BackgroundColor = draft.BackgroundColor;
        eventEntity.BackgroundEffect = draft.BackgroundEffect;
        eventEntity.BackgroundImageId = draft.BackgroundImageId;
        eventEntity.SourceTemplateId = draft.TemplateId;
        eventEntity.EventSeriesId = draft.EventSeriesId;
        eventEntity.SeriesOrder = draft.SeriesOrder;
        eventEntity.RegistrationPolicyId = draft.RegistrationPolicyId;
        eventEntity.ApplyScheduleTimeZone(timezoneId, _scheduleProjectionCalculator);

        await _participationConfigurationRepository.UpdateAsync(participationConfiguration, cancellationToken);
        await _eventRepository.Update(eventEntity);

        response.Success = true;
        response.Id = eventEntity.Id;
        response.Message = "Event draft updated successfully.";

        await _cache.RemoveAsync($"event:detail:{eventEntity.Id}", cancellationToken);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(eventEntity.TenantId), cancellationToken);

        return response;
    }
}
