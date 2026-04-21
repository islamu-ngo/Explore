// ABOUTME: Service for managing EventDay entities via API calls.
// ABOUTME: Provides CRUD operations for event days within an event.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Helpers;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public class EventDayService : IEventDayService
{
    private readonly IEventApiClient _client;
    private readonly ILogger<EventDayService> _logger;

    public EventDayService(IEventApiClient client, ILogger<EventDayService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ICollection<EventDayListDto>> GetDaysByEventAsync(Guid eventId)
    {
        try
        {
            var result = await _client.GetEventDaysByEventAsync(eventId);
            return result?.GetItems() ?? new List<EventDayListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching days for event {EventId}", eventId);
            return new List<EventDayListDto>();
        }
    }

    public async Task<EventDayDto?> GetDayByIdAsync(Guid dayId)
    {
        try
        {
            var result = await _client.GetEventDayByIdAsync(dayId);
            return result?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event day {DayId}", dayId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateDayAsync(CreateEventDayDto dto)
    {
        try
        {
            return await _client.CreateEventDayAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Error creating event day: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid { Success = false, Message = ex.Message };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateDayAsync(Guid id, UpdateEventDayDto dto)
    {
        try
        {
            return await _client.UpdateEventDayAsync(id, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Error updating event day {DayId}: {StatusCode}", id, ex.StatusCode);
            return new BaseCommandResponseOfGuid { Success = false, Message = ex.Message };
        }
    }

    public async Task<bool> DeleteDayAsync(Guid dayId)
    {
        try
        {
            await _client.DeleteEventDayAsync(dayId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event day {DayId}", dayId);
            return false;
        }
    }
}
