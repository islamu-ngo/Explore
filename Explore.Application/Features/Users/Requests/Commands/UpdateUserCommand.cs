using Explore.Application.DTOs.User;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands
{
    public class UpdateUserCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateUserDto UpdateUserDto { get; set; }
    }
}
