using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands;

[AuthorizeResource("event_session", PermissionAction.Update)]
public class DeleteEventSessionSpeakerCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
