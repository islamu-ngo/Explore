// ABOUTME: Handles rebuilding projection rows for a single event.
// ABOUTME: Delegates to RefreshForEventAsync which recomputes all projection rows for the event.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Handlers.Commands;

public class RebuildSingleEventCustomPropertyProjectionCommandHandler
    : IRequestHandler<RebuildSingleEventCustomPropertyProjectionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IUnitOfWork _unitOfWork;

    public RebuildSingleEventCustomPropertyProjectionCommandHandler(
        IEventCustomPropertyProjectionUpdater projectionUpdater,
        IUnitOfWork unitOfWork)
    {
        _projectionUpdater = projectionUpdater;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        RebuildSingleEventCustomPropertyProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        if (request.EventId == Guid.Empty)
        {
            response.Success = false;
            response.Message = "EventId is required.";
            response.Errors = ["EventId is required."];
            return response;
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await _projectionUpdater.RefreshForEventAsync(request.EventId, ct),
            cancellationToken);

        response.Success = true;
        response.Id = request.EventId;
        response.Message = "Event projection rows refreshed successfully.";

        return response;
    }
}
