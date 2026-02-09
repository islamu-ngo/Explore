using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Handlers.Commands;

public class DeleteEventSessionSpeakerCommandHandler : IRequestHandler<DeleteEventSessionSpeakerCommand, bool>
{
    private readonly IEventSessionSpeakerRepository _speakerRepository;

    public DeleteEventSessionSpeakerCommandHandler(IEventSessionSpeakerRepository speakerRepository)
    {
        _speakerRepository = speakerRepository;
    }

    public async Task<bool> Handle(DeleteEventSessionSpeakerCommand request, CancellationToken cancellationToken)
    {
        var speaker = await _speakerRepository.GetById(request.Id);

        if (speaker == null)
        {
            return false;
        }

        await _speakerRepository.Delete(speaker);

        return true;
    }
}
