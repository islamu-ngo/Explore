using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands;

public class UpdateIndexedDidCommand : IRequest<BaseCommandResponse<string>>
{
    public required UpdateIndexedDidDto IndexedDidDto { get; set; }
}
