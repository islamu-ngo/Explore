using System.Collections.Generic;
using System.Threading.Tasks;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IMadhabService
{
    Task<ICollection<MadhabListDto>> GetMadhabsAsync();
    Task<MadhabDto> GetMadhabByIdAsync(int id);
}
