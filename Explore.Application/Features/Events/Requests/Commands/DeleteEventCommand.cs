// ABOUTME: MediatR command for deleting an event by ID.
// ABOUTME: Carries the target event ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource("event", AuthorizationActions.Delete)]
public class DeleteEventCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
