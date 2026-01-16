using MediatR;
using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;

namespace Explore.Application.Features.Actors.Requests.Commands;

public class CreateActorCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateActorDto ActorDto { get; set; }
}
