// ABOUTME: MediatR query request for fetching a single sync state by ID.
// ABOUTME: Returns SyncStateDto.
using Explore.Application.DTOs.SyncState;
using MediatR;

namespace Explore.Application.Features.SyncStates.Requests.Queries;

public class GetSyncStateDetailsRequest : IRequest<SyncStateDto?>
{
    public int Id { get; set; }
}
