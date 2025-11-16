using System;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands
{
    public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateEventDto EventDto { get; set; }
    }
}
