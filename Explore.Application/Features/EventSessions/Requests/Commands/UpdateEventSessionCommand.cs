using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource("event_session", PermissionAction.Update)]
public class UpdateEventSessionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventSessionDto EventSessionDto { get; set; }

    string? ISecureRequest.ResourceId => EventSessionDto.Id.ToString();
}
