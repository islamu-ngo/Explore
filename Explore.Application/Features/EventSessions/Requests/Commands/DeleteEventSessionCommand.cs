using System;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

public class DeleteEventSessionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
