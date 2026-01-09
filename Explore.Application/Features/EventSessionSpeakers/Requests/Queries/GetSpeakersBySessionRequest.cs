using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionSpeaker;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Queries
{
    public class GetSpeakersBySessionRequest : IRequest<List<EventSessionSpeakerListDto>>
    {
        public Guid EventSessionId { get; set; }
    }
}
