using MediatR;
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Responses;

namespace Explore.Application.Features.UserExternalLogins.Requests.Commands
{
    public class UpdateUserExternalLoginCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateUserExternalLoginDto UserExternalLoginDto { get; set; }
    }
}
