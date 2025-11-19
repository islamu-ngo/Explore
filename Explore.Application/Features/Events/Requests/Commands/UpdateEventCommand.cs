using System;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands
{
    public class UpdateEventCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateEventDto EventDto { get; set; }
    }
}
