// ABOUTME: Handler for soft-deleting an event-level agenda item by Id.
// ABOUTME: Follows the pattern where delete returns BaseCommandResponse<Guid>.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Handlers.Commands;

public class DeleteEventAgendaItemCommandHandler : IRequestHandler<DeleteEventAgendaItemCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;

    public DeleteEventAgendaItemCommandHandler(IEventAgendaItemRepository eventAgendaItemRepository)
    {
        _eventAgendaItemRepository = eventAgendaItemRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeleteEventAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var agendaItem = await _eventAgendaItemRepository.GetById(request.Id);
        if (agendaItem == null)
        {
            response.Success = false;
            response.Message = "Event agenda item not found.";
            return response;
        }

        await _eventAgendaItemRepository.Delete(agendaItem);

        response.Success = true;
        response.Id = agendaItem.Id;
        response.Message = "Event agenda item deleted successfully.";

        return response;
    }
}
