// ABOUTME: MediatR query request for fetching a paginated indexed DID list.
// ABOUTME: Returns IEnumerable<IndexedDidListDto>.
using Explore.Application.DTOs.IndexedDid;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Queries;

public class GetIndexedDidListRequest : IRequest<List<IndexedDidListDto>>
{
}
