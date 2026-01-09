using System;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands
{
    public class CreateEventSessionAgendaItemCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateEventSessionAgendaItemDto AgendaItemDto { get; set; }
    }
}
