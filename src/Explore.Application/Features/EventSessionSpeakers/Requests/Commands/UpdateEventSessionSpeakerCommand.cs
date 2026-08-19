// ABOUTME: MediatR command for route-ID event-session speaker link updates.
// ABOUTME: Carries expected concurrency and grouped relationship update payload.
using System;
using System.Collections.Generic;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class UpdateEventSessionSpeakerCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventSessionSpeakerId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
    public required UpdateEventSessionSpeakerDto SpeakerDto { get; set; }
    public Guid EventSessionId { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(TenantId, EventId);
}
