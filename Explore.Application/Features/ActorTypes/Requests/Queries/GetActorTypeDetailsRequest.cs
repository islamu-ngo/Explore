using Explore.Application.DTOs.ActorType;
using MediatR;

namespace Explore.Application.Features.ActorTypes.Requests.Queries
{
    public class GetActorTypeDetailsRequest : IRequest<ActorTypeDto>
    {
        public int Id { get; set; }
    }
}
