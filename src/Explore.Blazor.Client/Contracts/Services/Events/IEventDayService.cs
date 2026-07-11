// ABOUTME: Contract for EventDay CRUD operations consumed by Blazor UI components.
// ABOUTME: Wraps the NSwag-generated IEventApiClient methods for EventDay entity.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface IEventDayService
{
    Task<ICollection<EventDayListDto>> GetDaysByEventAsync(Guid eventId);
    Task<EventDayDto?> GetDayByIdAsync(Guid dayId);
    Task<BaseCommandResponseOfGuid?> CreateDayAsync(CreateEventDayDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateDayAsync(Guid id, Guid expectedConcurrencyStamp, UpdateEventDayDto dto);
    Task<bool> DeleteDayAsync(Guid dayId);
}
