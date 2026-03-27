// ABOUTME: Single command for all actor updates using the null-check DTO pattern.
// ABOUTME: Each nullable DTO targets a specific update; the handler applies whichever is non-null.

using System;
using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Commands;

public class UpdateActorCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid Id { get; set; }

    public UpdateActorDto? ActorDto { get; set; }
    public UpdateActorAppearanceDto? AppearanceDto { get; set; }
}
