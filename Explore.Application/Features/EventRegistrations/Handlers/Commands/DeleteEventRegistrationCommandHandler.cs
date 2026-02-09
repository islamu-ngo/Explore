using System;
using System.Threading;
using System.Threading.Tasks;
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
        var eventRegistration = await _eventRegistrationRepository.GetById(request.Id);

        if (eventRegistration == null)
        {
            return false;
        }

        await _eventRegistrationRepository.Delete(eventRegistration);
        return true;
    }
}
