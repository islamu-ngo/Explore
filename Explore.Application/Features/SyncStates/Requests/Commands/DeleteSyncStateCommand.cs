using MediatR;

namespace Explore.Application.Features.SyncStates.Requests.Commands;

public class DeleteSyncStateCommand : IRequest<bool>
{
    public int Id { get; set; }
}
