using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;

[AuthorizeResource("event_session_agenda_item", PermissionAction.Create)]
public class CreateEventSessionAgendaItemCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventSessionAgendaItemDto AgendaItemDto { get; set; }

    string? ISecureRequest.ResourceId => AgendaItemDto.EventSessionId.ToString();
}
