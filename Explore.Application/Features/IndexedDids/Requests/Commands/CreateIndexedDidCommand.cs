using MediatR;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Responses;

namespace Explore.Application.Features.IndexedDids.Requests.Commands
{
    public class CreateIndexedDidCommand : IRequest<BaseCommandResponse<string>>
    {
        public CreateIndexedDidDto IndexedDidDto { get; set; }
    }
}
