using MediatR;
using Explore.Application.DTOs.SyncState;

namespace Explore.Application.Features.SyncStates.Requests.Queries
{
    public class GetSyncStateListRequest : IRequest<List<SyncStateListDto>>
    {
    }
}
