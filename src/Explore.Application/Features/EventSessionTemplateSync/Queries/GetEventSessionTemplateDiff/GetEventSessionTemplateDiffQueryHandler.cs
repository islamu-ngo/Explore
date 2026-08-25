// ABOUTME: Handles event-session-template diff requests by delegating to the explicit diff service and wrapping the DTO response.
// ABOUTME: Keeps the query path read-only while preserving the repo-wide BaseCommandResponse envelope convention.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventSessionTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionTemplateSync.Queries.GetEventSessionTemplateDiff;

public sealed class GetEventSessionTemplateDiffQueryHandler
    : IRequestHandler<GetEventSessionTemplateDiffQuery, BaseCommandResponse<TemplateDiffDto>>
{
    private readonly IEventSessionTemplateDiffService _diffService;

    public GetEventSessionTemplateDiffQueryHandler(IEventSessionTemplateDiffService diffService)
    {
        _diffService = diffService;
    }

    public async Task<BaseCommandResponse<TemplateDiffDto>> Handle(
        GetEventSessionTemplateDiffQuery request,
        CancellationToken cancellationToken)
    {
        var diff = await _diffService.ComputeDiffAsync(request.EventSessionId, request.TargetTemplateVersion, cancellationToken);
        return BaseCommandResponse.Success(diff, "Event session template diff retrieved successfully.");
    }
}
