using Explore.Application.DTOs.ActorKeyStore;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Queries;

public class GetActorKeyStoreDetailsRequest : IRequest<ActorKeyStoreDto>
{
    public Guid Id { get; set; }
}
