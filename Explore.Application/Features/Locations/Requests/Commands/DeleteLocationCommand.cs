using System;
using MediatR;

namespace Explore.Application.Features.Locations.Requests.Commands;

public class DeleteLocationCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
