using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Commands;

public class UpdateActorCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateActorDto ActorDto { get; set; }
}
