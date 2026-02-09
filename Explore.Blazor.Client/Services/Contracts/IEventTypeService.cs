using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IEventTypeService
{
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
}
