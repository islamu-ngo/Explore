// ABOUTME: MediatR command for creating a new event session.
// ABOUTME: Carries the CreateEventSessionDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource("event_session", PermissionAction.Create)]
public class CreateEventSessionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventSessionDto EventSessionDto { get; set; }

    string? ISecureRequest.ResourceId => EventSessionDto.EventId.ToString();
}
