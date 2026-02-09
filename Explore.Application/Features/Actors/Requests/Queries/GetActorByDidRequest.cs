using Explore.Application.DTOs.Actor;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorByDidRequest : IRequest<ActorDto>
{
    public required string Did { get; set; }
}
