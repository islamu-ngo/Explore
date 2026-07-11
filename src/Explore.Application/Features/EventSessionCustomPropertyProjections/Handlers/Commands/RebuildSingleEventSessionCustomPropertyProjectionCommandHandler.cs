// ABOUTME: Handles rebuilding projection rows for a single event session.
// ABOUTME: Delegates to RefreshForEventSessionAsync for session-scoped projection repair.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Handlers.Commands;

public class RebuildSingleEventSessionCustomPropertyProjectionCommandHandler
    : IRequestHandler<RebuildSingleEventSessionCustomPropertyProjectionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionCustomPropertyProjectionUpdater _projectionUpdater;
    private readonly IUnitOfWork _unitOfWork;

    public RebuildSingleEventSessionCustomPropertyProjectionCommandHandler(
        IEventSessionCustomPropertyProjectionUpdater projectionUpdater,
        IUnitOfWork unitOfWork)
    {
        _projectionUpdater = projectionUpdater;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        RebuildSingleEventSessionCustomPropertyProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        if (request.EventSessionId == Guid.Empty)
        {
            response.Success = false;
            response.Message = "EventSessionId is required.";
            response.Errors = ["EventSessionId is required."];
            return response;
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct => await _projectionUpdater.RefreshForEventSessionAsync(request.EventSessionId, ct),
            cancellationToken);

        response.Success = true;
        response.Id = request.EventSessionId;
        response.Message = "Event session projection rows refreshed successfully.";

        return response;
    }
}
