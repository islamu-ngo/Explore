// ABOUTME: Contract for EventAgendaItem CRUD operations consumed by Blazor UI components.
// ABOUTME: Wraps the NSwag-generated IEventApiClient methods for EventAgendaItem entity.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface IEventAgendaItemService
{
    Task<ICollection<EventAgendaItemListDto>> GetAgendaItemsByEventAsync(Guid eventId);
    Task<EventAgendaItemDto?> GetAgendaItemByIdAsync(Guid agendaItemId);
    Task<BaseCommandResponseOfGuid?> CreateAgendaItemAsync(CreateEventAgendaItemDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateAgendaItemAsync(Guid id, UpdateEventAgendaItemDto dto);
    Task<bool> DeleteAgendaItemAsync(Guid agendaItemId);
}
