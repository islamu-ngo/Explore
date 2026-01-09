using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionSpeaker;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Queries
{
    public class GetEventSessionSpeakerListRequest : IRequest<List<EventSessionSpeakerListDto>>
    {
    }
}
