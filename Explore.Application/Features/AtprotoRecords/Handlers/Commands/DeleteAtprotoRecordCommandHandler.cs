// ABOUTME: Handler for deleting an AT Protocol record.
// ABOUTME: Fetches record by ID and delegates deletion to the repository.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Handlers.Commands;

public class DeleteAtprotoRecordCommandHandler : IRequestHandler<DeleteAtprotoRecordCommand, bool>
{
    private readonly IAtprotoRecordRepository _atprotoRecordRepository;

    public DeleteAtprotoRecordCommandHandler(IAtprotoRecordRepository atprotoRecordRepository)
    {
        _atprotoRecordRepository = atprotoRecordRepository;
    }

    public async Task<bool> Handle(DeleteAtprotoRecordCommand request, CancellationToken cancellationToken)
    {
        var atprotoRecord = await _atprotoRecordRepository.GetById(request.Id);
        if (atprotoRecord == null)
        {
            return false;
        }

        await _atprotoRecordRepository.Delete(atprotoRecord);
        return true;
    }
}
