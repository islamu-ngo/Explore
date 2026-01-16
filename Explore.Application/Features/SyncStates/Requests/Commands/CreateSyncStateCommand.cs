using MediatR;
using Explore.Application.DTOs.SyncState;
using Explore.Application.Responses;

namespace Explore.Application.Features.SyncStates.Requests.Commands
{
    public class CreateSyncStateCommand : IRequest<BaseCommandResponse<int>>
    {
        public CreateSyncStateDto SyncStateDto { get; set; }
    }
}
