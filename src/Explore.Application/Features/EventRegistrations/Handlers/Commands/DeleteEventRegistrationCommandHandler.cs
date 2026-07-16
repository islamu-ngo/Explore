// ABOUTME: Handler for cancelling an event registration with a server-bound persisted owner snapshot.
// ABOUTME: Delegates to the repository's atomic owner-predicate cancellation and capacity-release path.
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
        if (request.ExpectedOwnerUserId is not { } expectedOwnerUserId)
            return false;

        return await _eventRegistrationRepository.CancelAndReleaseCapacityAsync(
            request.Id,
            expectedOwnerUserId,
            cancellationToken);
    }
}
