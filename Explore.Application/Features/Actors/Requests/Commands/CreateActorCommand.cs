using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Commands;

public class CreateActorCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateActorDto ActorDto { get; set; }
}
