using Explore.Application.DTOs.IndexedDid;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Requests.Queries;

public class GetIndexedDidListRequest : IRequest<List<IndexedDidListDto>>
{
}
