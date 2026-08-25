// ABOUTME: MediatR command for synchronizing a user from an external identity provider.
// ABOUTME: Carries the UserDto from the identity provider.
using Explore.Application.DTOs.User;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Users.Requests.Commands;

public sealed record SyncUserCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UserDto UserDto { get; init; }
}
