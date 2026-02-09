using Explore.Application.DTOs.SyncState;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.SyncStates.Requests.Commands;

public class UpdateSyncStateCommand : IRequest<BaseCommandResponse<int>>
{
    public required UpdateSyncStateDto SyncStateDto { get; set; }
}
