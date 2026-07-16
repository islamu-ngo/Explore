// ABOUTME: Event-scoped organizer queries for exact program-section collection and detail reads.
// ABOUTME: Both requests authorize against the parent event before exposing location and room fields.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionGroup;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetManagedEventSessionGroupsByEventRequest
    : IRequest<List<EventSessionGroupListDto>>, ISecureRequest
{
    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetManagedEventSessionGroupDetailRequest
    : IRequest<EventSessionGroupDto?>, ISecureRequest
{
    public Guid EventId { get; set; }
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
