// ABOUTME: MediatR query request for fetching a paginated list of actor key stores.
// ABOUTME: Returns IEnumerable<ActorKeyStoreListDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.ActorKeyStore;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Requests.Queries;

public class GetActorKeyStoreListRequest : IRequest<List<ActorKeyStoreListDto>>
{
}
