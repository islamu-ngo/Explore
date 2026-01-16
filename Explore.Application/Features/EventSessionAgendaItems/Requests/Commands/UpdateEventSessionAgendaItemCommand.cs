using System;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands
{
    public class UpdateEventSessionAgendaItemCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateEventSessionAgendaItemDto AgendaItemDto { get; set; }
    }
}
