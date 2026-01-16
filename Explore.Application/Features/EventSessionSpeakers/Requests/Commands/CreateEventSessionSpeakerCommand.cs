using System;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands
{
    public class CreateEventSessionSpeakerCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateEventSessionSpeakerDto SpeakerDto { get; set; }
    }
}
