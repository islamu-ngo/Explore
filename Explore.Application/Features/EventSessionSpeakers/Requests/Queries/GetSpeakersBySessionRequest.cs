// ABOUTME: MediatR query for fetching all speakers in a session.
// ABOUTME: Returns IEnumerable<ActorDto>.
using System;
using System.Collections.Generic;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionSpeaker;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Queries;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class GetSpeakersBySessionRequest : IRequest<List<EventSessionSpeakerListDto>>, ISecureRequest
{
    public Guid EventSessionId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId.ToString();
}
