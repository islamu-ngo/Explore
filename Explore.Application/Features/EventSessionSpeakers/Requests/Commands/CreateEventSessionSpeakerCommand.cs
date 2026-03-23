// ABOUTME: MediatR command for adding a speaker to an event session.
// ABOUTME: Carries the CreateEventSessionSpeakerDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands;

[AuthorizeResource("event_session", PermissionAction.Update)]
public class CreateEventSessionSpeakerCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventSessionSpeakerDto SpeakerDto { get; set; }

    string? ISecureRequest.ResourceId => SpeakerDto.EventSessionId.ToString();
}
