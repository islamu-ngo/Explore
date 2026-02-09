using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Commands;

public class DeleteIndexedDidCommand : IRequest<bool>
{
    public required string Did { get; set; }
}
