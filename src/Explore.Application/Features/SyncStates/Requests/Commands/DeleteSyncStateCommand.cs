// ABOUTME: MediatR command for deleting a sync state record by ID.
// ABOUTME: Carries the target sync state ID.
using MediatR;

namespace Explore.Application.Features.SyncStates.Requests.Commands;

public class DeleteSyncStateCommand : IRequest<bool>
{
    public int Id { get; set; }
}
