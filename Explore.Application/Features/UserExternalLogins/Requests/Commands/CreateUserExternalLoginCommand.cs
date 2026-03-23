// ABOUTME: MediatR command for creating an external login link.
// ABOUTME: Carries the CreateUserExternalLoginDto payload.
using Explore.Application.DTOs.UserExternalLogin;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.UserExternalLogins.Requests.Commands;

public class CreateUserExternalLoginCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateUserExternalLoginDto UserExternalLoginDto { get; set; }
}
