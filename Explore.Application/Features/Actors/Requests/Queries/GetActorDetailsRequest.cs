using Explore.Application.DTOs.Actor;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorDetailsRequest : IRequest<ActorDto>
{
    public Guid Id { get; set; }
}
