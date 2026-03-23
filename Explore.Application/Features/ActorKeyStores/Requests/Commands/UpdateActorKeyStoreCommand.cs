// ABOUTME: MediatR command for updating an existing actor key store entry.
// ABOUTME: Carries the UpdateActorKeyStoreDto payload.
using Explore.Application.DTOs.ActorKeyStore;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Commands;

public class UpdateActorKeyStoreCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateActorKeyStoreDto ActorKeyStoreDto { get; set; }
}
