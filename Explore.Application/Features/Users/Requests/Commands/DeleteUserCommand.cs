using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

public class DeleteUserCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
}
