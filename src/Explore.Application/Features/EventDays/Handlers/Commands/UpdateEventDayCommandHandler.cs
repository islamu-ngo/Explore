// ABOUTME: Handler for grouped EventDay PATCH updates.
// ABOUTME: Applies explicit groups, checks concurrency, and invalidates parent event caches.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay;
using Explore.Application.DTOs.EventDay.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventDays.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventDays.Handlers.Commands;

public class UpdateEventDayCommandHandler : IRequestHandler<UpdateEventDayCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventRepository _eventRepository;
    private readonly HybridCache _cache;

    public UpdateEventDayCommandHandler(
        IEventDayRepository eventDayRepository,
        IEventRepository eventRepository,
        HybridCache cache)
    {
        _eventDayRepository = eventDayRepository;
        _eventRepository = eventRepository;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventDayCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventDayDtoValidator(_eventRepository, _eventDayRepository);
        var validationResult = await validator.ValidateAsync(request.EventDayDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event day update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventDay = await _eventDayRepository.GetById(request.EventDayId);
        if (eventDay == null)
        {
            response.Success = false;
            response.Message = "Event day not found.";
            return response;
        }

        if (eventDay.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The event day was modified by another request. Reload and retry.",
                nameof(EventDay),
                eventDay.Id.ToString());
        }

        validationResult = await validator.ValidateAsync(
            request.EventDayDto,
            eventDay.Id,
            eventDay.EventId,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event day update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var previousEventId = eventDay.EventId;
        var parentEventId = request.EventDayDto.Event?.EventId ?? eventDay.EventId;
        var parentEvent = await _eventRepository.GetById(parentEventId);
        if (parentEvent == null || parentEvent.TenantId != eventDay.TenantId)
        {
            response.Success = false;
            response.Message = "Event does not belong to the same tenant as the event day.";
            return response;
        }

        var eventChanged = previousEventId != parentEvent.Id;
        ApplyEvent(eventDay, request.EventDayDto.Event);
        ApplyLocalDate(eventDay, request.EventDayDto.LocalDate);
        ApplyLabel(eventDay, request.EventDayDto.Label);
        ApplyDescription(eventDay, request.EventDayDto.Description);
        ApplyBannerText(eventDay, request.EventDayDto.BannerText);
        ApplyBannerImage(eventDay, request.EventDayDto.BannerImage);
        ApplyPublication(eventDay, request.EventDayDto.Publication);
        ApplySortOrder(eventDay, request.EventDayDto.SortOrder);
        ApplyRegistration(eventDay, request.EventDayDto.Registration);

        await _eventDayRepository.Update(eventDay);

        if (eventChanged)
        {
            await _cache.RemoveAsync($"event:detail:{previousEventId}", cancellationToken);
        }

        await _cache.RemoveAsync($"event:detail:{parentEvent.Id}", cancellationToken);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), cancellationToken);

        response.Success = true;
        response.Id = eventDay.Id;
        response.Message = "Event day updated successfully.";

        return response;
    }

    private static void ApplyEvent(EventDay eventDay, UpdateEventDayEventDto? group)
    {
        if (group is not null)
        {
            eventDay.EventId = group.EventId;
        }
    }

    private static void ApplyLocalDate(EventDay eventDay, UpdateEventDayLocalDateDto? group)
    {
        if (group is not null)
        {
            eventDay.LocalDate = group.Value;
        }
    }

    private static void ApplyLabel(EventDay eventDay, UpdateEventDayLabelDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventDay.Label = group.Value.Value;
        }
    }

    private static void ApplyDescription(EventDay eventDay, UpdateEventDayDescriptionDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventDay.Description = group.Value.Value;
        }
    }

    private static void ApplyBannerText(EventDay eventDay, UpdateEventDayBannerTextDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventDay.BannerText = group.Value.Value;
        }
    }

    private static void ApplyBannerImage(EventDay eventDay, UpdateEventDayBannerImageDto? group)
    {
        if (group?.Value.HasValue == true)
        {
            eventDay.BannerImageId = group.Value.Value;
        }
    }

    private static void ApplyPublication(EventDay eventDay, UpdateEventDayPublicationDto? group)
    {
        if (group is not null)
        {
            eventDay.IsPublished = group.IsPublished;
        }
    }

    private static void ApplySortOrder(EventDay eventDay, UpdateEventDaySortOrderDto? group)
    {
        if (group is not null)
        {
            eventDay.SortOrder = group.Value;
        }
    }

    private static void ApplyRegistration(EventDay eventDay, UpdateEventDayRegistrationDto? group)
    {
        if (group is not null)
        {
            eventDay.AllowsDayScopeRegistration = group.AllowsDayScopeRegistration;
        }
    }
}
