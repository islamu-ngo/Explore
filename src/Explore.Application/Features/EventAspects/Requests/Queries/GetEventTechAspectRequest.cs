// ABOUTME: Query request to get the Tech aspect for an event.
// ABOUTME: Returns null if the event doesn't have a Tech aspect.

namespace Explore.Application.Features.EventAspects.Requests.Queries;

using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventAspects;
using MediatR;

/// <summary>
/// Request to retrieve the Tech aspect for a specific event.
/// </summary>
public sealed record GetEventTechAspectRequest(Guid EventId) : IRequest<EventTechAspectDto?>;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed record GetManagedEventTechAspectRequest : IRequest<EventTechAspectDto?>, ISecureRequest
{
    public Guid EventId { get; init; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
