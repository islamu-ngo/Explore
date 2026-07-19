// ABOUTME: Handler for deleting an event session.
// ABOUTME: Fetches session by ID and delegates deletion to the repository.
using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Services;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public class DeleteEventSessionCommandHandler : IRequestHandler<DeleteEventSessionCommand, bool>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;

    public DeleteEventSessionCommandHandler(
        IEventSessionRepository eventSessionRepository,
        IUnitOfWork unitOfWork,
        EventLocationAttachmentService eventLocationAttachmentService)
    {
        _eventSessionRepository = eventSessionRepository;
        _unitOfWork = unitOfWork;
        _eventLocationAttachmentService = eventLocationAttachmentService;
    }

    public async Task<bool> Handle(DeleteEventSessionCommand request, CancellationToken cancellationToken)
    {
        var eventSession = await _eventSessionRepository.GetById(request.Id);

        if (eventSession == null)
        {
            return false;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            Guid? eventLocationId = eventSession.EventLocationId;
            eventSession.DetachEventLocationForDeletion();
            await _eventSessionRepository.Delete(eventSession);
            await _eventLocationAttachmentService.DetachIfUnreferencedAsync(
                eventLocationId,
                token);
        }, cancellationToken);

        return true;
    }
}
