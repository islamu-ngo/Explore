// ABOUTME: Handles event-template diff requests by delegating to the explicit diff service and wrapping the DTO response.
// ABOUTME: Keeps the query path read-only while preserving the repo-wide BaseCommandResponse envelope convention.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventTemplateSync;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTemplateSync.Queries.GetEventTemplateDiff;

public sealed class GetEventTemplateDiffQueryHandler
    : IRequestHandler<GetEventTemplateDiffQuery, BaseCommandResponse<TemplateDiffDto>>
{
    private readonly IEventTemplateDiffService _diffService;

    public GetEventTemplateDiffQueryHandler(IEventTemplateDiffService diffService)
    {
        _diffService = diffService;
    }

    public async Task<BaseCommandResponse<TemplateDiffDto>> Handle(
        GetEventTemplateDiffQuery request,
        CancellationToken cancellationToken)
    {
        var diff = await _diffService.ComputeDiffAsync(request.EventId, request.TargetTemplateVersion, cancellationToken);
        return new BaseCommandResponse<TemplateDiffDto>
        {
            Success = true,
            Id = diff,
            Message = "Event template diff retrieved successfully."
        };
    }
}
