// ABOUTME: Command handler to delete the Islamic aspect from an event.
// ABOUTME: Permanently removes the aspect data (hard delete).

namespace Explore.Application.Features.EventAspects.Handlers.Commands;

using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventAspects.Requests.Commands;
using MediatR;

/// <summary>
/// Handler for deleting the Islamic aspect from an event.
/// </summary>
public class DeleteEventIslamicAspectCommandHandler : IRequestHandler<DeleteEventIslamicAspectCommand, bool>
{
    private readonly IEventIslamicAspectRepository _islamicAspectRepository;

    public DeleteEventIslamicAspectCommandHandler(IEventIslamicAspectRepository islamicAspectRepository)
    {
        _islamicAspectRepository = islamicAspectRepository;
    }

    public async Task<bool> Handle(DeleteEventIslamicAspectCommand request, CancellationToken cancellationToken)
    {
        var aspect = await _islamicAspectRepository.GetById(request.EventId);

        if (aspect == null)
        {
            return false;
        }

        // Hard delete the aspect (aspects don't implement soft delete)
        await _islamicAspectRepository.HardDelete(aspect);

        return true;
    }
}
