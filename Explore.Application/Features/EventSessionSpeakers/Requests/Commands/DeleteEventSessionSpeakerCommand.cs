using System;
using MediatR;

namespace Explore.Application.Features.EventSessionSpeakers.Requests.Commands
{
    public class DeleteEventSessionSpeakerCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
