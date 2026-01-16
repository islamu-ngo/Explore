using Explore.Blazor.Client.Clients;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IActorService
{
    Task<ICollection<ActorListDto>> GetActorsAsync();
    Task<ActorDto> GetActorByIdAsync(Guid id);
}
