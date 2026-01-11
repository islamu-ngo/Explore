using MediatR;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Responses;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Commands
{
    public class CreateUserAuthenticationTokenCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateUserAuthenticationTokenDto UserAuthenticationTokenDto { get; set; }
    }
}
