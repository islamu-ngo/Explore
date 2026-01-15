using Explore.Blazor.Client.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Explore.Blazor.Client.Services.Contracts;

public interface IMadhabService
{
    Task<ICollection<MadhabListDto>> GetMadhabsAsync();
    Task<MadhabDto> GetMadhabByIdAsync(int id);
}
