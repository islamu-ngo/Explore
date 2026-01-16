using MediatR;
using Explore.Application.DTOs.ActorKeyStore;
using System.Collections.Generic;

namespace Explore.Application.Features.ActorKeyStores.Requests.Queries
{
    public class GetActorKeyStoreListRequest : IRequest<List<ActorKeyStoreListDto>>
    {
    }
}
