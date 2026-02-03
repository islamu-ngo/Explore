// ABOUTME: Query handler to retrieve the Islamic aspect for an event.
// ABOUTME: Returns the aspect with navigation properties (Madhab, PrimaryLanguage) or null.

namespace Explore.Application.Features.EventAspects.Handlers.Queries;

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Features.EventAspects.Requests.Queries;
using MediatR;

/// <summary>
/// Handler for retrieving the Islamic aspect of an event.
/// </summary>
public class GetEventIslamicAspectRequestHandler : IRequestHandler<GetEventIslamicAspectRequest, EventIslamicAspectDto?>
{
    private readonly IEventIslamicAspectRepository _islamicAspectRepository;
    private readonly IMapper _mapper;

    public GetEventIslamicAspectRequestHandler(
        IEventIslamicAspectRepository islamicAspectRepository,
        IMapper mapper)
    {
        _islamicAspectRepository = islamicAspectRepository;
        _mapper = mapper;
    }

    public async Task<EventIslamicAspectDto?> Handle(GetEventIslamicAspectRequest request, CancellationToken cancellationToken)
    {
        var aspect = await _islamicAspectRepository.GetByEventIdWithDetails(request.EventId);

        if (aspect == null)
        {
            return null;
        }

        return _mapper.Map<EventIslamicAspectDto>(aspect);
    }
}
