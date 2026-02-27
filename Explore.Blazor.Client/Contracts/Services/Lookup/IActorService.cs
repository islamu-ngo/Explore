// ABOUTME: Interface for actor service operations.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IActorService
{
    Task<ICollection<ActorListDto>> GetActorsAsync();
    Task<ActorDto?> GetActorByIdAsync(Guid id);
}
