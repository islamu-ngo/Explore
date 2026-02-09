using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Commands;

public class CreateActorKeyStoreCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateActorKeyStoreDto ActorKeyStoreDto { get; set; }
}
