using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessions.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public class DeleteEventSessionCommandHandler : IRequestHandler<DeleteEventSessionCommand, bool>
{
    private readonly IEventSessionRepository _eventSessionRepository;

    public DeleteEventSessionCommandHandler(IEventSessionRepository eventSessionRepository)
    {
        _eventSessionRepository = eventSessionRepository;
    }

    public async Task<bool> Handle(DeleteEventSessionCommand request, CancellationToken cancellationToken)
    {
        var eventSession = await _eventSessionRepository.GetById(request.Id);

        if (eventSession == null)
        {
            return false;
        }

        await _eventSessionRepository.Delete(eventSession);

        return true;
    }
}
