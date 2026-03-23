// ABOUTME: Handler for deleting an agenda item from an event session.
// ABOUTME: Fetches agenda item by ID and delegates deletion.
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Commands;

public class DeleteEventSessionAgendaItemCommandHandler : IRequestHandler<DeleteEventSessionAgendaItemCommand, bool>
{
    private readonly IEventSessionAgendaItemRepository _agendaItemRepository;

    public DeleteEventSessionAgendaItemCommandHandler(IEventSessionAgendaItemRepository agendaItemRepository)
    {
        _agendaItemRepository = agendaItemRepository;
    }

    public async Task<bool> Handle(DeleteEventSessionAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var agendaItem = await _agendaItemRepository.GetById(request.Id);

        if (agendaItem == null)
        {
            return false;
        }

        await _agendaItemRepository.Delete(agendaItem);

        return true;
    }
}
