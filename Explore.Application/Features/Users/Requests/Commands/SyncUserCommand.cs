using Explore.Application.DTOs.User;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

public class SyncUserCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UserDto UserDto { get; set; }
}
