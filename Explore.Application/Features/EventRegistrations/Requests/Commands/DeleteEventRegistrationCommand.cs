using System;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

public class DeleteEventRegistrationCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
