using Explore.Application.DTOs.UserAuthenticationToken;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.UserAuthenticationTokens.Requests.Commands;

public class UpdateUserAuthenticationTokenCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateUserAuthenticationTokenDto UserAuthenticationTokenDto { get; set; }
}
