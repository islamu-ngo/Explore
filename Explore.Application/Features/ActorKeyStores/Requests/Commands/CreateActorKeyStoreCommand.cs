// ABOUTME: MediatR command for creating a new actor key store entry.
// ABOUTME: Carries the CreateActorKeyStoreDto payload.
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Commands;

public class CreateActorKeyStoreCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateActorKeyStoreDto ActorKeyStoreDto { get; set; }
}
