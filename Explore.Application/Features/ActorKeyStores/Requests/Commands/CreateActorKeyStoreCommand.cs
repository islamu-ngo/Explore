using MediatR;
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Responses;

namespace Explore.Application.Features.ActorKeyStores.Requests.Commands
{
    public class CreateActorKeyStoreCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateActorKeyStoreDto ActorKeyStoreDto { get; set; }
    }
}
