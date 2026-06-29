// ABOUTME: Handles grouped Event PATCH updates with concurrency, explicit mapping, and cache invalidation.
// ABOUTME: Loads the schedule graph once, applies present property groups, saves once, and evicts Event caches.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
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

    public UpdateEventCommandHandler(
        IEventRepository eventRepository,
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

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var validator = new UpdateEventDtoValidator(
            _audienceAgeRepository,
            _audienceGenderRepository,
            _eventTypeRepository,
            _visibilityTypeRepository,
            _eventFormatRepository,
            _storageObjectRepository,
            _eventSeriesRepository,
            _eventRegistrationPolicyRepository);

        var validationResult = await validator.ValidateAsync(request.UpdateEventDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event update failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var eventEntity = await _eventRepository.GetScheduleGraphForUpdateAsync(request.EventId, cancellationToken);
        if (eventEntity is null)
        {
            response.Success = false;
            response.Message = "Event not found.";
            return response;
        }

        if (eventEntity.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event changed since it was loaded. Refresh the event and try again.",
                "event",
                eventEntity.Id.ToString());
        }

        var update = request.UpdateEventDto;
        ApplyTitle(eventEntity, update.Title);
        ApplySubtitle(eventEntity, update.Subtitle);
        ApplyDescription(eventEntity, update.Description);
        ApplyContent(eventEntity, update.Content);
        ApplySlug(eventEntity, update.Slug);
        ApplyEventType(eventEntity, update.EventType);
        ApplyAudienceGender(eventEntity, update.AudienceGender);
        ApplyAudienceAge(eventEntity, update.AudienceAge);
        ApplyPrice(eventEntity, update.Price);
        ApplyCurrencyCode(eventEntity, update.CurrencyCode);
        ApplyFeaturedImage(eventEntity, update.FeaturedImage);
        ApplyRegistrationRequired(eventEntity, update.RegistrationRequired);
        ApplyExternalRegistrationUrl(eventEntity, update.ExternalRegistrationUrl);
        ApplyVisibility(eventEntity, update.Visibility);
        ApplyFormat(eventEntity, update.Format);
        ApplyMadhab(eventEntity, update.Madhab);
        ApplyTimezone(eventEntity, update.Timezone, update.EventTimeZone);
        ApplyEventUrl(eventEntity, update.EventUrl);
        ApplyBackgroundColor(eventEntity, update.BackgroundColor);
        ApplyBackgroundEffect(eventEntity, update.BackgroundEffect);
        ApplyBackgroundImage(eventEntity, update.BackgroundImage);
        ApplyTemplate(eventEntity, update.SourceTemplate);
        ApplySeries(eventEntity, update.SeriesMembership);
        ApplySeriesOrder(eventEntity, update.SeriesOrder);
        ApplyRegistrationPolicy(eventEntity, update.RegistrationPolicy);

        await _eventRepository.Update(eventEntity);

        response.Success = true;
        response.Id = eventEntity.Id;
        response.Message = "Event updated successfully.";

        await _cache.RemoveAsync($"event:detail:{eventEntity.Id}", cancellationToken);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(eventEntity.TenantId), cancellationToken);

        return response;
    }

    private static void ApplyTitle(Explore.Domain.Event eventEntity, UpdateEventTitleDto? update)
    {
        if (update is not null)
        {
            eventEntity.Title = update.Value;
        }
    }

    private static void ApplySubtitle(Explore.Domain.Event eventEntity, UpdateEventSubtitleDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.Subtitle = update.Value.Value;
        }
    }

    private static void ApplyDescription(Explore.Domain.Event eventEntity, UpdateEventDescriptionDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.Description = update.Value.Value;
        }
    }

    private static void ApplyContent(Explore.Domain.Event eventEntity, UpdateEventContentDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.Content = update.Value.Value;
        }
    }

    private static void ApplySlug(Explore.Domain.Event eventEntity, UpdateEventSlugDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.Slug = update.Value.Value;
        }
    }

    private static void ApplyEventType(Explore.Domain.Event eventEntity, UpdateEventEventTypeDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.EventTypeId = update.Value.Value;
        }
    }

    private static void ApplyAudienceGender(Explore.Domain.Event eventEntity, UpdateEventAudienceGenderDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.AudienceGenderId = update.Value.Value;
        }
    }

    private static void ApplyAudienceAge(Explore.Domain.Event eventEntity, UpdateEventAudienceAgeDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.AudienceAgeId = update.Value.Value;
        }
    }

    private static void ApplyPrice(Explore.Domain.Event eventEntity, UpdateEventPriceDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.Price = update.Value.Value;
        }
    }

    private static void ApplyCurrencyCode(Explore.Domain.Event eventEntity, UpdateEventCurrencyCodeDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.CurrencyCode = update.Value.Value;
        }
    }

    private static void ApplyFeaturedImage(Explore.Domain.Event eventEntity, UpdateEventFeaturedImageDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.FeaturedImageId = update.Value.Value;
        }
    }

    private static void ApplyRegistrationRequired(Explore.Domain.Event eventEntity, UpdateEventRegistrationRequiredDto? update)
    {
        if (update is not null)
        {
            eventEntity.IsRegistrationRequired = update.Value;
        }
    }

    private static void ApplyExternalRegistrationUrl(Explore.Domain.Event eventEntity, UpdateEventExternalRegistrationUrlDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.ExternalRegistrationUrl = update.Value.Value;
        }
    }

    private static void ApplyVisibility(Explore.Domain.Event eventEntity, UpdateEventVisibilityDto? update)
    {
        if (update is not null)
        {
            eventEntity.VisibilityTypeId = update.Value;
        }
    }

    private static void ApplyFormat(Explore.Domain.Event eventEntity, UpdateEventFormatDto? update)
    {
        if (update is not null)
        {
            eventEntity.EventFormatId = update.Value;
        }
    }

    private static void ApplyMadhab(Explore.Domain.Event eventEntity, UpdateEventMadhabDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.MadhabId = update.Value.Value;
        }
    }

    private void ApplyTimezone(
        Explore.Domain.Event eventEntity,
        UpdateEventTimezoneDto? timezone,
        UpdateEventEventTimeZoneDto? eventTimeZone)
    {
        if (timezone?.Value.HasValue != true && eventTimeZone?.Value.HasValue != true)
        {
            return;
        }

        var requested = eventTimeZone?.Value.HasValue == true
            ? eventTimeZone.Value.Value
            : timezone?.Value.Value;

        var timezoneId = ScheduleTimeZoneResolver.NormalizeOrUtc(requested);
        eventEntity.Timezone = timezoneId;
        eventEntity.EventTimeZoneId = timezoneId;
        eventEntity.ApplyScheduleTimeZone(timezoneId, _scheduleProjectionCalculator);
    }

    private static void ApplyEventUrl(Explore.Domain.Event eventEntity, UpdateEventUrlDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.EventUrl = update.Value.Value;
        }
    }

    private static void ApplyBackgroundColor(Explore.Domain.Event eventEntity, UpdateEventBackgroundColorDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.BackgroundColor = update.Value.Value;
        }
    }

    private static void ApplyBackgroundEffect(Explore.Domain.Event eventEntity, UpdateEventBackgroundEffectDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.BackgroundEffect = update.Value.Value;
        }
    }

    private static void ApplyBackgroundImage(Explore.Domain.Event eventEntity, UpdateEventBackgroundImageDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.BackgroundImageId = update.Value.Value;
        }
    }

    private static void ApplyTemplate(Explore.Domain.Event eventEntity, UpdateEventSourceTemplateDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.SourceTemplateId = update.Value.Value;
        }
    }

    private static void ApplySeries(Explore.Domain.Event eventEntity, UpdateEventSeriesMembershipDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.EventSeriesId = update.Value.Value;
        }
    }

    private static void ApplySeriesOrder(Explore.Domain.Event eventEntity, UpdateEventSeriesOrderDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.SeriesOrder = update.Value.Value;
        }
    }

    private static void ApplyRegistrationPolicy(Explore.Domain.Event eventEntity, UpdateEventRegistrationPolicyDto? update)
    {
        if (update?.Value.HasValue == true)
        {
            eventEntity.RegistrationPolicyId = update.Value.Value;
        }
    }
}
