using Explore.Application.DTOs.SyncState;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.SyncStates.Requests.Commands;

public class CreateSyncStateCommand : IRequest<BaseCommandResponse<int>>
{
    public required CreateSyncStateDto SyncStateDto { get; set; }
}
