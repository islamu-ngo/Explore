// ABOUTME: Handles projection status query for event session custom-property projections.
// ABOUTME: Mirrors event projection status handler for session scope.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Handlers.Queries;

public class GetEventSessionCustomPropertyProjectionStatusQueryHandler
    : IRequestHandler<GetEventSessionCustomPropertyProjectionStatusQuery, BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>>
{
    private readonly ICustomPropertyProjectionStatusRepository _statusRepository;
    private readonly IMapper _mapper;

    public GetEventSessionCustomPropertyProjectionStatusQueryHandler(
        ICustomPropertyProjectionStatusRepository statusRepository,
        IMapper mapper)
    {
        _statusRepository = statusRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>> Handle(
        GetEventSessionCustomPropertyProjectionStatusQuery request,
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
            IEventSessionCustomPropertyProjectionUpdater.ProjectionName,
            IEventSessionCustomPropertyProjectionUpdater.ProjectionVersion,
            request.TenantId,
            cancellationToken);

        var dtos = status is not null
            ? new List<ProjectionStatusDto> { _mapper.Map<ProjectionStatusDto>(status) }
            : new List<ProjectionStatusDto>();

        response.Success = true;
        response.Id = dtos;
        response.Message = "Session projection status retrieved.";

        return response;
    }
}
