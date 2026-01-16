using MediatR;
using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Responses;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Commands
{
    public class UpdateUserAuthenticationTokenCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateUserAuthenticationTokenDto UserAuthenticationTokenDto { get; set; }
    }
}
