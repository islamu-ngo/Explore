using Explore.Application.DTOs.SyncState;
using MediatR;

namespace Explore.Application.Features.SyncStates.Requests.Queries;

public class GetSyncStateListRequest : IRequest<List<SyncStateListDto>>
{
}
