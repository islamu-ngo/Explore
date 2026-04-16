// ABOUTME: Handles query for all projection rows of a specific event session with optional exposure ceiling.
// ABOUTME: Maps session projection entities to EventSessionCustomPropertyProjectionDto.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Handlers.Queries;

public class GetEventSessionCustomPropertyProjectionsForSessionQueryHandler
    : IRequestHandler<GetEventSessionCustomPropertyProjectionsForSessionQuery, BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>>
{
    private readonly IEventSessionCustomPropertyProjectionRepository _projectionRepository;
    private readonly IMapper _mapper;

    public GetEventSessionCustomPropertyProjectionsForSessionQueryHandler(
        IEventSessionCustomPropertyProjectionRepository projectionRepository,
        IMapper mapper)
    {
        _projectionRepository = projectionRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>> Handle(
        GetEventSessionCustomPropertyProjectionsForSessionQuery request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>();

        if (request.EventSessionId == Guid.Empty)
        {
            response.Success = false;
            response.Message = "EventSessionId is required.";
            response.Errors = ["EventSessionId is required."];
            return response;
        }

        var projections = await _projectionRepository.GetForSessionAsync(
            request.EventSessionId,
            request.ExposureCeiling,
            cancellationToken);

        var dtos = _mapper.Map<List<EventSessionCustomPropertyProjectionDto>>(projections);

        response.Success = true;
        response.Id = dtos;
        response.Message = "Session projection rows retrieved.";

        return response;
    }
}
