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
        private readonly IOrganizationRepository _organizationRepository;

        public DeleteEventCommandHandler(IEventRepository eventRepository, IOrganizationRepository organizationRepository)
        {
            _eventRepository = eventRepository;
            _organizationRepository = organizationRepository;
        }

        public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetById(request.Id);
            if (@event == null)
            {
                return false;
            }

            // Verify ownership through organization
            var organization = await _organizationRepository.GetById(@event.OrganizationId);
            if (organization == null || organization.CreatedByUserId != request.UserId)
            {
                // User doesn't own the organization, cannot delete the event
                return false;
            }

            await _eventRepository.Delete(@event);
            return true;
        }
    }
}
