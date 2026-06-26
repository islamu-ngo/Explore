// ABOUTME: Handler that builds the full agenda projection for an event by merging sessions and agenda items into day groups.
// ABOUTME: Groups by LocalStartDate, enriches with EventDay metadata, and sorts entries by start time within each day.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Agenda;
using Explore.Application.Features.Agenda.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Agenda.Handlers.Queries;

public class GetEventAgendaProjectionRequestHandler : IRequestHandler<GetEventAgendaProjectionRequest, EventAgendaProjectionDto?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;

    public GetEventAgendaProjectionRequestHandler(
        IEventRepository eventRepository,
        IEventDayRepository eventDayRepository,
        IEventSessionRepository eventSessionRepository,
        IEventAgendaItemRepository eventAgendaItemRepository)
    {
        _eventRepository = eventRepository;
        _eventDayRepository = eventDayRepository;
        _eventSessionRepository = eventSessionRepository;
        _eventAgendaItemRepository = eventAgendaItemRepository;
    }

    public async Task<EventAgendaProjectionDto?> Handle(GetEventAgendaProjectionRequest request, CancellationToken cancellationToken)
    {
        var parentEvent = await _eventRepository.GetById(request.EventId);
        if (parentEvent == null || !IsPublicAgendaEligible(parentEvent))
            return null;

        var eventDays = await _eventDayRepository.GetByEventAsync(request.EventId, cancellationToken);
        var sessions = await _eventSessionRepository.GetPublicSessionsByEventAsync(request.EventId, cancellationToken);
        var agendaItems = await _eventAgendaItemRepository.GetByEventAsync(request.EventId, cancellationToken);

        var entries = new List<AgendaScheduleEntryDto>();

        foreach (var session in sessions)
        {
            if (session.StartTime is not { } startTime ||
                session.EndTime is not { } endTime ||
                session.LocalStartDate is not { } localStartDate ||
                session.LocalStartTime is not { } localStartTime ||
                session.LocalEndTime is not { } localEndTime ||
                session.LocalStartMinuteOfDay is not { } localStartMinuteOfDay ||
                session.LocalEndMinuteOfDay is not { } localEndMinuteOfDay)
            {
                continue;
            }

            entries.Add(new AgendaScheduleEntryDto
            {
                Id = session.Id,
                EntryType = "Session",
                Title = session.Title ?? string.Empty,
                Description = session.Description,
                StartTime = startTime,
                EndTime = endTime,
                LocalStartDate = localStartDate,
                LocalStartTime = localStartTime,
                LocalEndTime = localEndTime,
                LocalStartMinuteOfDay = localStartMinuteOfDay,
                LocalEndMinuteOfDay = localEndMinuteOfDay,
                RoomId = session.RoomId,
                LocationId = session.LocationId,
                MaxAudienceAttendees = session.MaxAudienceAttendees,
                CurrentAudienceAttendees = session.CurrentAudienceAttendees,
                RegistrationModeId = session.RegistrationModeId,
                RegistrationModeFullName = session.RegistrationMode?.FullName,
                Price = session.Price,
                CurrencyCode = session.CurrencyCode,
                SortOrder = session.SortOrder
            });
        }

        foreach (var item in agendaItems)
        {
            entries.Add(new AgendaScheduleEntryDto
            {
                Id = item.Id,
                EntryType = "AgendaItem",
                Title = item.Title,
                Description = item.Description,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                LocalStartDate = item.LocalStartDate,
                LocalStartTime = item.LocalStartTime,
                LocalEndTime = item.LocalEndTime,
                LocalStartMinuteOfDay = item.LocalStartMinuteOfDay,
                LocalEndMinuteOfDay = item.LocalEndMinuteOfDay,
                RoomId = item.RoomId,
                LocationId = item.LocationId,
                KindId = item.KindId,
                KindFullName = item.Kind?.FullName,
                SortOrder = item.SortOrder
            });
        }

        var entriesByDate = entries
            .GroupBy(e => e.LocalStartDate)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.LocalStartMinuteOfDay).ThenBy(e => e.SortOrder).ToList());

        var daysByDate = eventDays.ToDictionary(d => d.LocalDate);

        var allDates = new HashSet<DateOnly>(daysByDate.Keys);
        foreach (var date in entriesByDate.Keys)
            allDates.Add(date);

        var dayGroups = new List<AgendaDayGroupDto>();
        foreach (var date in allDates.OrderBy(d => d))
        {
            daysByDate.TryGetValue(date, out var eventDay);
            entriesByDate.TryGetValue(date, out var dayEntries);

            dayGroups.Add(new AgendaDayGroupDto
            {
                EventDayId = eventDay?.Id,
                LocalDate = date,
                Label = eventDay?.Label,
                Description = eventDay?.Description,
                IsPublished = eventDay?.IsPublished ?? true,
                SortOrder = eventDay?.SortOrder ?? 0,
                AllowsDayScopeRegistration = eventDay?.AllowsDayScopeRegistration ?? false,
                Entries = dayEntries ?? []
            });
        }

        dayGroups = dayGroups
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.LocalDate)
            .ToList();

        return new EventAgendaProjectionDto
        {
            EventId = parentEvent.Id,
            EventTitle = parentEvent.Title,
            Timezone = parentEvent.EventTimeZoneId ?? parentEvent.Timezone,
            Days = dayGroups
        };
    }

    private static bool IsPublicAgendaEligible(Event parentEvent)
    {
        return parentEvent.EventStatusId == (int)EventStatusEnum.Published &&
            parentEvent.VisibilityTypeId == (int)VisibilityTypeEnum.Public;
    }
}
