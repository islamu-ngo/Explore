using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public class EventSessionAgendaItemService : IEventSessionAgendaItemService
{
    private readonly IEventApiClient _client;
    private readonly ILogger<EventSessionAgendaItemService> _logger;

    public EventSessionAgendaItemService(IEventApiClient client, ILogger<EventSessionAgendaItemService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ICollection<EventSessionAgendaItemListDto>> GetAgendaItemsBySessionAsync(Guid sessionId)
    {
        try
        {
            return await _client.BySession3Async(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching agenda items for session {SessionId}", sessionId);
            return new List<EventSessionAgendaItemListDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateAgendaItemAsync(CreateEventSessionAgendaItemDto item)
    {
        try
        {
            return await _client.EventsessionagendaitemPOSTAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agenda item for session {SessionId}", item.EventSessionId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateAgendaItemAsync(Guid id, UpdateEventSessionAgendaItemDto item)
    {
        try
        {
            return await _client.EventsessionagendaitemPUTAsync(id, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agenda item {AgendaItemId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAgendaItemAsync(Guid agendaItemId)
    {
        try
        {
            await _client.EventsessionagendaitemDELETEAsync(agendaItemId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting agenda item {AgendaItemId}", agendaItemId);
            return false;
        }
    }
}
