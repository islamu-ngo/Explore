// ABOUTME: MediatR command for grouped actor PATCH updates.
// ABOUTME: Carries route authority, If-Match concurrency, and the wrapper DTO.

using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Actor;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Commands;

[AuthorizeResource(ResourceKinds.Actor, AuthorizationActions.Update)]
public class UpdateActorCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid ActorId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateActorDto UpdateActorDto { get; set; }

    string? ISecureRequest.ResourceId => ActorId.ToString();
}
