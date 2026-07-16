// ABOUTME: Query handler returning a single event session by ID.
// ABOUTME: Maps EventSession entity to EventSessionDto.
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Queries;

public class GetEventSessionDetailsRequestHandler : IRequestHandler<GetEventSessionDetailsRequest, EventSessionDto?>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IMapper _mapper;

    public GetEventSessionDetailsRequestHandler(
        IEventSessionRepository eventSessionRepository,
        IMapper mapper)
    {
        _eventSessionRepository = eventSessionRepository;
        _mapper = mapper;
    }

    public async Task<EventSessionDto?> Handle(GetEventSessionDetailsRequest request, CancellationToken cancellationToken)
    {
        var eventSession = await _eventSessionRepository.GetPublicSessionWithDetailsAsync(
            request.Id,
            cancellationToken);
        return PublicEventSessionLocationRedactor.Redact(_mapper.Map<EventSessionDto>(eventSession));
    }
}

internal static class PublicEventSessionLocationRedactor
{
    public static EventSessionDto? Redact(EventSessionDto? dto)
    {
        if (dto is null)
            return null;

        dto.LocationId = null;
        dto.LocationFullName = null;
        dto.LocationAddress = null;
        dto.LocationCity = null;
        dto.LocationCountry = null;
        dto.RoomId = null;
        dto.RoomName = null;
        return dto;
    }

    public static List<EventSessionListDto> Redact(List<EventSessionListDto> dtos)
    {
        foreach (var dto in dtos)
        {
            dto.LocationId = null;
            dto.LocationFullName = null;
            dto.LocationCity = null;
            dto.RoomId = null;
            dto.RoomName = null;
        }

        return dtos;
    }
}
