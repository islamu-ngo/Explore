using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Commands
{
    public class DeleteActorKeyStoreCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
