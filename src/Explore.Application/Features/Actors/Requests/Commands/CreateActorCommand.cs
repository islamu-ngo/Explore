// ABOUTME: MediatR command for creating a new actor.
// ABOUTME: Carries the CreateActorDto payload.
using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Commands;

public sealed record CreateActorCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateActorDto ActorDto { get; init; }
}
