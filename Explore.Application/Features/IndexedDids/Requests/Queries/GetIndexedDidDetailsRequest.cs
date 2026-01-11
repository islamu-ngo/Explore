using MediatR;
using Explore.Application.DTOs.IndexedDid;

namespace Explore.Application.Features.IndexedDids.Requests.Queries
{
    public class GetIndexedDidDetailsRequest : IRequest<IndexedDidDto?>
    {
        public string Did { get; set; }
    }
}
