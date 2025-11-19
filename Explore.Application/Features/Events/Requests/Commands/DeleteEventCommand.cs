using System;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands
{
    public class DeleteEventCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
    }
}
