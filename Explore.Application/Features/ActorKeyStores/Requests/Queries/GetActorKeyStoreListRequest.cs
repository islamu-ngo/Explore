using System.Collections.Generic;
using Explore.Application.DTOs.ActorKeyStore;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Queries;

public class GetActorKeyStoreListRequest : IRequest<List<ActorKeyStoreListDto>>
{
}
