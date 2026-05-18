// ABOUTME: MediatR command for updating a session-speaker link.
// ABOUTME: Carries the UpdateEventSessionSpeakerDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class UpdateEventSessionSpeakerCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventSessionSpeakerDto SpeakerDto { get; set; }

    string? ISecureRequest.ResourceId => SpeakerDto.EventSessionId.ToString();
}
