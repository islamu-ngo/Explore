// ABOUTME: Handles query for all projection rows of a specific event, optionally filtered by exposure ceiling.
// ABOUTME: Maps projection entities to EventCustomPropertyProjectionDto for admin inspection.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Handlers.Queries;

public class GetEventCustomPropertyProjectionsForEventQueryHandler
    : IRequestHandler<GetEventCustomPropertyProjectionsForEventQuery, BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>>
{
    private readonly IEventCustomPropertyProjectionRepository _projectionRepository;
    private readonly IMapper _mapper;

    public GetEventCustomPropertyProjectionsForEventQueryHandler(
        IEventCustomPropertyProjectionRepository projectionRepository,
        IMapper mapper)
    {
        _projectionRepository = projectionRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>> Handle(
        GetEventCustomPropertyProjectionsForEventQuery request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>();

        if (request.EventId == Guid.Empty)
        {
            response.Success = false;
            response.Message = "EventId is required.";
            response.Errors = ["EventId is required."];
            return response;
        }

        var projections = await _projectionRepository.GetForEventAsync(
            request.EventId,
            request.ExposureCeiling,
            cancellationToken);

        var dtos = _mapper.Map<List<EventCustomPropertyProjectionDto>>(projections);

        response.Success = true;
        response.Id = dtos;
        response.Message = "Projection rows retrieved.";

        return response;
    }
}
