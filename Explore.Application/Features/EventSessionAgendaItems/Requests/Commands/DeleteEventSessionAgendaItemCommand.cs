using System;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Commands
{
    public class DeleteEventSessionAgendaItemCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
