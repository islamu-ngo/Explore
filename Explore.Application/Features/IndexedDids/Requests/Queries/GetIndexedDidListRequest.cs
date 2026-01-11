using MediatR;
using Explore.Application.DTOs.IndexedDid;

namespace Explore.Application.Features.IndexedDids.Requests.Queries
{
    public class GetIndexedDidListRequest : IRequest<List<IndexedDidListDto>>
    {
    }
}
