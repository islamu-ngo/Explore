using Explore.Application.DTOs.ActorType;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.ActorTypes.Requests.Queries
{
    public class GetActorTypeListRequest : IRequest<List<ActorTypeListDto>>
    {
    }
}
