// ABOUTME: Contract for LocationRoom CRUD operations consumed by Blazor UI components.
// ABOUTME: Wraps the NSwag-generated IEventApiClient methods for LocationRoom entity.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface ILocationRoomService
{
    Task<ICollection<LocationRoomListDto>> GetRoomsByLocationAsync(Guid locationId);
    Task<LocationRoomDto?> GetRoomByIdAsync(Guid roomId);
    Task<BaseCommandResponseOfGuid?> CreateRoomAsync(CreateLocationRoomDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateRoomAsync(Guid id, UpdateLocationRoomDto dto);
    Task<bool> DeleteRoomAsync(Guid roomId);
}
