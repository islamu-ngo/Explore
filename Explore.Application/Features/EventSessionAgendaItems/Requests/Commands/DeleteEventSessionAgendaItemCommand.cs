using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;

[AuthorizeResource("event_session_agenda_item", PermissionAction.Delete)]
public class DeleteEventSessionAgendaItemCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
