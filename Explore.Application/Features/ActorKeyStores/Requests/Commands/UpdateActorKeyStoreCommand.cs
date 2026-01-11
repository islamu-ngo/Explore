using MediatR;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Responses;

namespace Explore.Application.Features.ActorKeyStores.Requests.Commands
{
    public class UpdateActorKeyStoreCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateActorKeyStoreDto ActorKeyStoreDto { get; set; }
    }
}
