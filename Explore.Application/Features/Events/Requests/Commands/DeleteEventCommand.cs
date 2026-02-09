using System;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

public class DeleteEventCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
}
