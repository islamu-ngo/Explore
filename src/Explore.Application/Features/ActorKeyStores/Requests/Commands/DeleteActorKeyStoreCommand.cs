// ABOUTME: MediatR command for deleting an actor key store entry.
// ABOUTME: Carries the target entity ID.
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Commands;

public class DeleteActorKeyStoreCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
