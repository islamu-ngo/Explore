using MediatR;
using Explore.Application.DTOs.SyncState;
using Explore.Application.Responses;

namespace Explore.Application.Features.SyncStates.Requests.Commands
{
    public class UpdateSyncStateCommand : IRequest<BaseCommandResponse<int>>
    {
        public UpdateSyncStateDto SyncStateDto { get; set; }
    }
}
