using MediatR;
using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;

namespace Explore.Application.Features.Actors.Requests.Commands;

public class UpdateActorCommand : IRequest<BaseCommandResponse<Guid>>
{
    public UpdateActorDto ActorDto { get; set; }
}
