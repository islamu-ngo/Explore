// ABOUTME: MediatR command for removing a speaker from an event session.
// ABOUTME: Carries the junction record ID.
using System;
using System.Collections.Generic;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventSession, AuthorizationActions.Update)]
public class DeleteEventSessionSpeakerCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }
    public Guid EventSessionId { get; set; }
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => EventSessionId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(TenantId, EventId);
}
