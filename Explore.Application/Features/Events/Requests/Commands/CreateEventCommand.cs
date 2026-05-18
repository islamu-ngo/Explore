// ABOUTME: MediatR command for creating a new event.
// ABOUTME: Carries the canonical CreateEventRequest graph payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Create)]
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventRequest Request { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        Request.OrganizationId.HasValue
            ? new Dictionary<string, object> { ["organizationId"] = Request.OrganizationId.Value.ToString() }
            : null;
}
