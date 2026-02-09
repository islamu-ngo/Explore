using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands;

public class CreateIndexedDidCommand : IRequest<BaseCommandResponse<string>>
{
    public required CreateIndexedDidDto IndexedDidDto { get; set; }
}
