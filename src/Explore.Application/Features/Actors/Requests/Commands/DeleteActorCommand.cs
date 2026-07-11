// ABOUTME: MediatR command for deleting an actor by ID.
// ABOUTME: Carries the target actor ID.
using MediatR;

namespace Explore.Application.Features.Actors.Requests.Commands;

public class DeleteActorCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
