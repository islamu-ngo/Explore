// ABOUTME: MediatR command for adding a speaker to an event session.
// ABOUTME: Carries the CreateEventSessionSpeakerDto payload.
using System;
using System.Collections.Generic;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class CreateEventSessionSpeakerCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventSessionSpeakerDto SpeakerDto { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => SpeakerDto.EventSessionId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["eventId"] = EventId.ToString()
    };
}
