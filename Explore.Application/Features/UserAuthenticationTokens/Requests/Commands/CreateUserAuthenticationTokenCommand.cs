using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;

public class CreateUserAuthenticationTokenCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateUserAuthenticationTokenDto UserAuthenticationTokenDto { get; set; }
}
