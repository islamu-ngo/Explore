using MediatR;
using Explore.Application.DTOs.Actor;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorByDidRequest : IRequest<ActorDto>
{
    public string Did { get; set; }
}
