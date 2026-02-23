using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource("event", PermissionAction.Create)]
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventDto EventDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        EventDto.OrganizationId != Guid.Empty
            ? new Dictionary<string, object> { ["organizationId"] = EventDto.OrganizationId.ToString() }
            : null;
}
