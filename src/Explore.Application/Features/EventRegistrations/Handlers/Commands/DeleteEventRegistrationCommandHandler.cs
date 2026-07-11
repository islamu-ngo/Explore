// ABOUTME: Handler for cancelling an event registration.
// ABOUTME: Fetches registration by ID and delegates deletion to the repository.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Commands;

public class DeleteEventRegistrationCommandHandler : IRequestHandler<DeleteEventRegistrationCommand, bool>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;

    public DeleteEventRegistrationCommandHandler(IEventRegistrationRepository eventRegistrationRepository)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
    }

    public async Task<bool> Handle(DeleteEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        return await _eventRegistrationRepository.CancelAndReleaseCapacityAsync(request.Id, cancellationToken);
    }
}
