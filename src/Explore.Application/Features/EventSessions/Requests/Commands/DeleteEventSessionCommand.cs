// ABOUTME: MediatR command for deleting an event session by ID.
// ABOUTME: Carries the target session ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Delete)]
public class DeleteEventSessionCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
