// ABOUTME: Blazor service contract for public and event-authorized session agenda reads.
// ABOUTME: Separates redacted public presentation from exact management data flows.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface IEventSessionAgendaItemService
{
    Task<ICollection<EventSessionAgendaItemListDto>> GetAgendaItemsBySessionAsync(Guid sessionId);
    Task<ICollection<EventSessionAgendaItemListDto>> GetManagedAgendaItemsBySessionAsync(Guid eventId, Guid sessionId);
    Task<BaseCommandResponseOfGuid?> CreateAgendaItemAsync(CreateEventSessionAgendaItemDto item);
    Task<BaseCommandResponseOfGuid?> UpdateAgendaItemAsync(Guid id, UpdateEventSessionAgendaItemDto item);
    Task<bool> DeleteAgendaItemAsync(Guid agendaItemId);
}
