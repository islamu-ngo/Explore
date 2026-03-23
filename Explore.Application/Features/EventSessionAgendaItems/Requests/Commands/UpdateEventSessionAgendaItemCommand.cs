// ABOUTME: MediatR command for updating an agenda item.
// ABOUTME: Carries the UpdateEventSessionAgendaItemDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;

[AuthorizeResource("event_session_agenda_item", PermissionAction.Update)]
public class UpdateEventSessionAgendaItemCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventSessionAgendaItemDto AgendaItemDto { get; set; }

    string? ISecureRequest.ResourceId => AgendaItemDto.Id.ToString();
}
