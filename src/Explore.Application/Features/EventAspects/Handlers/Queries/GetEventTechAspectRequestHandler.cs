// ABOUTME: Query handler to retrieve the Tech aspect for an event.
// ABOUTME: Returns the aspect or null if the event doesn't have one.

namespace Explore.Application.Features.EventAspects.Handlers.Queries;

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Features.EventAspects.Requests.Queries;
using MediatR;

/// <summary>
/// Handler for retrieving the Tech aspect of an event.
/// </summary>
public class GetEventTechAspectRequestHandler : IRequestHandler<GetEventTechAspectRequest, EventTechAspectDto?>
{
    private readonly IEventTechAspectRepository _techAspectRepository;
    private readonly IMapper _mapper;

    public GetEventTechAspectRequestHandler(
        IEventTechAspectRepository techAspectRepository,
        IMapper mapper)
    {
        _techAspectRepository = techAspectRepository;
        _mapper = mapper;
    }

    public async Task<EventTechAspectDto?> Handle(GetEventTechAspectRequest request, CancellationToken cancellationToken)
    {
        var aspect = await _techAspectRepository.GetByEventId(request.EventId);

        if (aspect == null)
        {
            return null;
        }

        return _mapper.Map<EventTechAspectDto>(aspect);
    }
}
