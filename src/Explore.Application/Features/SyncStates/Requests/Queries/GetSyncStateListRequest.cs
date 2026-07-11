// ABOUTME: MediatR query request for fetching a paginated sync state list.
// ABOUTME: Returns IEnumerable<SyncStateListDto>.
using Explore.Application.DTOs.SyncState;
using MediatR;

namespace Explore.Application.Features.SyncStates.Requests.Queries;

public class GetSyncStateListRequest : IRequest<List<SyncStateListDto>>
{
}
