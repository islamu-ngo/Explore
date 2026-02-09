using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Requests.Commands;

public class UpdateUserExternalLoginCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateUserExternalLoginDto UserExternalLoginDto { get; set; }
}
