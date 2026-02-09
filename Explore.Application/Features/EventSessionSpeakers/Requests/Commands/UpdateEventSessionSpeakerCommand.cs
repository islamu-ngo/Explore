using System;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands;

public class UpdateEventSessionSpeakerCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateEventSessionSpeakerDto SpeakerDto { get; set; }
}
