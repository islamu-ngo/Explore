using MediatR;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Responses;

namespace Explore.Application.Features.IndexedDids.Requests.Commands
{
    public class UpdateIndexedDidCommand : IRequest<BaseCommandResponse<string>>
    {
        public UpdateIndexedDidDto IndexedDidDto { get; set; }
    }
}
