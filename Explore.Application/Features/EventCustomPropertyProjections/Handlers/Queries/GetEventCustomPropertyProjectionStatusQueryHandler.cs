// ABOUTME: Handles projection status query returning current state of event custom-property projections for a tenant.
// ABOUTME: Maps from CustomPropertyProjectionStatus entities to ProjectionStatusDto.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.CustomProperties;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Handlers.Queries;

public class GetEventCustomPropertyProjectionStatusQueryHandler
    : IRequestHandler<GetEventCustomPropertyProjectionStatusQuery, BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>>
{
    private readonly ICustomPropertyProjectionStatusRepository _statusRepository;
    private readonly ICustomPropertyProjectionDirtyScopeRepository _dirtyScopeRepository;
    private readonly IMapper _mapper;

    public GetEventCustomPropertyProjectionStatusQueryHandler(
        ICustomPropertyProjectionStatusRepository statusRepository,
        ICustomPropertyProjectionDirtyScopeRepository dirtyScopeRepository,
        IMapper mapper)
    {
        _statusRepository = statusRepository;
        _dirtyScopeRepository = dirtyScopeRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>> Handle(
        GetEventCustomPropertyProjectionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>();

        if (request.TenantId == Guid.Empty)
        {
            response.Success = false;
            response.Message = "TenantId is required.";
            response.Errors = ["TenantId is required."];
            return response;
        }

        var status = await _statusRepository.GetAsync(
            IEventCustomPropertyProjectionUpdater.ProjectionName,
            IEventCustomPropertyProjectionUpdater.ProjectionVersion,
            request.TenantId,
            cancellationToken);

        var dtos = new List<ProjectionStatusDto>();
        if (status is not null)
        {
            var dto = _mapper.Map<ProjectionStatusDto>(status);
            var pendingDirtyScopes = await _dirtyScopeRepository.CountPendingAsync(
                IEventCustomPropertyProjectionUpdater.ProjectionName,
                IEventCustomPropertyProjectionUpdater.ProjectionVersion,
                request.TenantId,
                cancellationToken);

            CustomPropertyProjectionStatusSignals.Apply(dto, pendingDirtyScopes, DateTimeOffset.UtcNow);
            dtos.Add(dto);
        }

        response.Success = true;
        response.Id = dtos;
        response.Message = "Projection status retrieved.";

        return response;
    }
}
