// ABOUTME: MediatR command for updating an event session.
// ABOUTME: Carries the UpdateEventSessionDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

[AuthorizeResource("event_session", AuthorizationActions.Update)]
public class UpdateEventSessionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventSessionDto EventSessionDto { get; set; }

    string? ISecureRequest.ResourceId => EventSessionDto.Id.ToString();
}
