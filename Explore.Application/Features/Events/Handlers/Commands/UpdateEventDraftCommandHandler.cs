// ABOUTME: Applies public draft-event updates explicitly without AutoMapper broad-field ownership leaks.
// ABOUTME: Preserves status, actor, tenant, and session-derived projection fields as server-owned state.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public sealed class UpdateEventDraftCommandHandler : IRequestHandler<UpdateEventDraftCommand, BaseCommandResponse<Guid>>
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
    private readonly HybridCache _cache;

    public UpdateEventDraftCommandHandler(
        IEventRepository eventRepository,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IVisibilityTypeRepository visibilityTypeRepository,
        IEventFormatRepository eventFormatRepository,
        IStorageObjectRepository storageObjectRepository,
        IEventSeriesRepository eventSeriesRepository,
        IEventRegistrationPolicyRepository eventRegistrationPolicyRepository,
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

        var eventEntity = await _eventRepository.GetById(request.Id);
        if (eventEntity is null)
        {
            response.Success = false;
            response.Message = "Event not found.";
            return response;
        }

        var draft = request.Draft;
        eventEntity.Title = draft.Title;
        eventEntity.Subtitle = draft.Subtitle;
        eventEntity.Description = draft.Description;
        eventEntity.Slug = draft.Slug;
        eventEntity.EventTypeId = draft.EventTypeId;
        eventEntity.AudienceGenderId = draft.AudienceGenderId;
        eventEntity.AudienceAgeId = draft.AudienceAgeId;
        eventEntity.Price = draft.Price;
        eventEntity.CurrencyCode = draft.CurrencyCode;
        eventEntity.FeaturedImageId = draft.FeaturedImageId;
        eventEntity.IsRegistrationRequired = draft.IsRegistrationRequired;
        eventEntity.ExternalRegistrationUrl = draft.ExternalRegistrationUrl;
        eventEntity.VisibilityTypeId = draft.VisibilityTypeId;
        eventEntity.EventFormatId = draft.EventFormatId;
        eventEntity.MadhabId = draft.MadhabId;
        eventEntity.Timezone = draft.Timezone ?? draft.EventTimeZoneId;
        eventEntity.EventTimeZoneId = draft.EventTimeZoneId ?? draft.Timezone;
        eventEntity.EventUrl = draft.EventUrl;
        eventEntity.BackgroundColor = draft.BackgroundColor;
        eventEntity.BackgroundEffect = draft.BackgroundEffect;
        eventEntity.BackgroundImageId = draft.BackgroundImageId;
        eventEntity.SourceTemplateId = draft.TemplateId;
        eventEntity.EventSeriesId = draft.EventSeriesId;
        eventEntity.SeriesOrder = draft.SeriesOrder;
        eventEntity.RegistrationPolicyId = draft.RegistrationPolicyId;

        await _eventRepository.Update(eventEntity);

        response.Success = true;
        response.Id = eventEntity.Id;
        response.Message = "Event draft updated successfully.";

        await _cache.RemoveAsync($"event:detail:{eventEntity.Id}", cancellationToken);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(eventEntity.TenantId), cancellationToken);

        return response;
    }
}
