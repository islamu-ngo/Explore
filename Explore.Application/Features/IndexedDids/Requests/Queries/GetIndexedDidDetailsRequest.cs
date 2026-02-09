using Explore.Application.DTOs.IndexedDid;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Queries;

public class GetIndexedDidDetailsRequest : IRequest<IndexedDidDto?>
{
    public required string Did { get; set; }
}
