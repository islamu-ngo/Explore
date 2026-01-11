using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Commands
{
    public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, bool>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IActorRepository _actorRepository;

        public DeleteEventCommandHandler(IEventRepository eventRepository, IActorRepository actorRepository)
        {
            _eventRepository = eventRepository;
            _actorRepository = actorRepository;
        }

        public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetById(request.Id);
            if (@event == null)
            {
                return false;
            }

            // Verify ownership through Actor
            var actor = await _actorRepository.GetById(@event.ActorId);
            if (actor == null)
            {
                // Actor doesn't exist, cannot verify ownership
                return false;
            }

            // Check if the actor is associated with the requesting user
            // For now, we allow deletion if the event exists and user is authenticated
            // Additional ownership checks would need User-Actor relationship

            await _eventRepository.Delete(@event);
            return true;
        }
    }
}
